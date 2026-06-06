using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitDetailsPreloadService : IDisposable
{
    private readonly CommitGraphRepository _commitGraphRepository;
    private readonly CommitDetailsService _commitDetailsService;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _switchLock = new(1, 1);
    private Guid? _activeRepositoryId;
    private CancellationTokenSource? _activeCancellation;
    private Task? _activeTask;
    private bool _disposed;

    public CommitDetailsPreloadService(
        CommitGraphRepository commitGraphRepository,
        CommitDetailsService commitDetailsService)
    {
        _commitGraphRepository = commitGraphRepository;
        _commitDetailsService = commitDetailsService;
    }

    public async Task StartOrSwitchAsync(
        Guid repositoryId,
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _switchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            if (_activeRepositoryId == repositoryId && _activeTask is { IsCompleted: false })
            {
                return;
            }

            var previousCancellation = _activeCancellation;
            var previousTask = _activeTask;
            previousCancellation?.Cancel();
            _activeRepositoryId = null;
            _activeCancellation = null;
            _activeTask = null;

            await WaitForTaskAsync(previousTask, cancellationToken).ConfigureAwait(false);
            previousCancellation?.Dispose();

            ThrowIfDisposed();
            var nextCancellation = new CancellationTokenSource();
            _activeRepositoryId = repositoryId;
            _activeCancellation = nextCancellation;
            _activeTask = Task.Run(
                () => RunPreloadAsync(repositoryId, repositoryPath, nextCancellation.Token),
                CancellationToken.None);
        }
        finally
        {
            _switchLock.Release();
        }
    }

    public async Task CancelActiveAsync(CancellationToken cancellationToken)
    {
        await _switchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var cancellationToStop = _activeCancellation;
            var taskToStop = _activeTask;
            _activeRepositoryId = null;
            _activeCancellation = null;
            _activeTask = null;

            cancellationToStop?.Cancel();
            await WaitForTaskAsync(taskToStop, cancellationToken).ConfigureAwait(false);
            cancellationToStop?.Dispose();
        }
        finally
        {
            _switchLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancellationTokenSource? cancellationToStop;
        lock (_gate)
        {
            cancellationToStop = _activeCancellation;
            _activeRepositoryId = null;
            _activeCancellation = null;
            _activeTask = null;
        }

        cancellationToStop?.Cancel();
        cancellationToStop?.Dispose();
        _switchLock.Dispose();
    }

    private async Task RunPreloadAsync(
        Guid repositoryId,
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var processedRows = new HashSet<string>(StringComparer.Ordinal);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rows = await _commitGraphRepository
                    .GetCachedCommitsAsync(repositoryId, cancellationToken)
                    .ConfigureAwait(false);

                var foundNewRow = false;
                foreach (var row in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!processedRows.Add(row.Id))
                    {
                        continue;
                    }

                    foundNewRow = true;
                    if (!GitObjectId.TryParse(row.Hash, out var commitId))
                    {
                        continue;
                    }

                    if (await _commitGraphRepository.HasCommitDetailsAsync(repositoryId, row.Hash, cancellationToken)
                            .ConfigureAwait(false))
                    {
                        continue;
                    }

                    try
                    {
                        await _commitDetailsService
                            .GetCommitDetailsAsync(repositoryId, repositoryPath, commitId, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch when (!cancellationToken.IsCancellationRequested)
                    {
                    }
                }

                if (!foundNewRow)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
        }
    }

    private static async Task WaitForTaskAsync(Task? task, CancellationToken cancellationToken)
    {
        if (task == null)
        {
            return;
        }

        try
        {
            await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CommitDetailsPreloadService));
        }
    }
}
