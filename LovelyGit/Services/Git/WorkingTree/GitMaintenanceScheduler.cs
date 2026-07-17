using System.Threading.Channels;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class GitMaintenanceScheduler : BackgroundService, IGitMaintenanceScheduler
{
    private static readonly TimeSpan DefaultSettleDelay = TimeSpan.FromMilliseconds(500);
    private readonly TimeSpan _settleDelay;
    private readonly Func<string, CancellationToken, Task> _runMaintenance;
    private readonly ILogger<GitMaintenanceScheduler>? _logger;
    private readonly Channel<string> _requests = Channel.CreateBounded<string>(
        new BoundedChannelOptions(32)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

    public GitMaintenanceScheduler(
        GitCliService gitCliService,
        ILogger<GitMaintenanceScheduler>? logger = null)
        : this(
            async (repositoryPath, cancellationToken) =>
            {
                await gitCliService.ExecuteBufferedAsync(
                    ["maintenance", "run", "--auto", "--quiet"],
                    repositoryPath,
                    validateExitCode: false,
                    cancellationToken).ConfigureAwait(false);
            },
            DefaultSettleDelay,
            logger)
    {
    }

    internal GitMaintenanceScheduler(
        Func<string, CancellationToken, Task> runMaintenance,
        TimeSpan settleDelay,
        ILogger<GitMaintenanceScheduler>? logger = null)
    {
        _runMaintenance = runMaintenance;
        _settleDelay = settleDelay;
        _logger = logger;
    }

    public void Schedule(string repositoryPath)
    {
        _requests.Writer.TryWrite(repositoryPath);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var repositoryPath in _requests.Reader.ReadAllAsync(stoppingToken))
            {
                if (_settleDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_settleDelay, stoppingToken).ConfigureAwait(false);
                }

                try
                {
                    await _runMaintenance(repositoryPath, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger?.LogDebug(exception, "Deferred Git maintenance failed for {RepositoryPath}.", repositoryPath);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
