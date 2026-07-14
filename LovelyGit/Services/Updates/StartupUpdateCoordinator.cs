using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Updates;

internal sealed class StartupUpdateCoordinator
{
    private readonly IApplicationUpdateClient _client;
    private readonly Action<Exception> _reportFailure;

    public StartupUpdateCoordinator(
        IApplicationUpdateClient client,
        Action<Exception>? reportFailure = null)
    {
        _client = client;
        _reportFailure = reportFailure ?? ReportFailure;
    }

    public static StartupUpdateCoordinator Create(
        Func<IApplicationUpdateClient> createClient,
        Action<Exception>? reportFailure = null)
    {
        var reporter = reportFailure ?? ReportFailure;
        try
        {
            return new StartupUpdateCoordinator(createClient(), reporter);
        }
        catch (Exception exception)
        {
            reporter(exception);
            return new StartupUpdateCoordinator(DisabledUpdateClient.Instance, reporter);
        }
    }

    public void ApplyPendingUpdate(string[] restartArguments)
    {
        try
        {
            if (_client.IsInstalled)
            {
                _client.TryApplyPendingUpdate(restartArguments);
            }
        }
        catch (Exception exception)
        {
            _reportFailure(exception);
        }
    }

    public async Task DownloadAvailableUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_client.IsInstalled)
            {
                await _client.DownloadAvailableUpdateAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _reportFailure(exception);
        }
    }

    private static void ReportFailure(Exception exception)
    {
        Trace.TraceWarning("Velopack update operation failed: {0}", exception);
        Console.Error.WriteLine("Velopack update operation failed: {0}", exception);
    }

    private sealed class DisabledUpdateClient : IApplicationUpdateClient
    {
        public static DisabledUpdateClient Instance { get; } = new();

        public bool IsInstalled => false;

        public bool TryApplyPendingUpdate(string[] restartArguments) => false;

        public Task DownloadAvailableUpdateAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
