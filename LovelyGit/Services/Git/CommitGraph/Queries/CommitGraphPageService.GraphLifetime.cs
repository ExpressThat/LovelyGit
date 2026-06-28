namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;

internal sealed partial class CommitGraphPageService
{
    private static readonly TimeSpan ActiveGraphIdleCloseDelay = TimeSpan.FromSeconds(2);

    private void ScheduleGraphClose(Guid repositoryId)
    {
        var work = new ActiveGraphCloseWork();
        lock (_cacheWorkLock)
        {
            CancelScheduledGraphCloseCore(repositoryId);
            _activeGraphCloseWork[repositoryId] = work;
            work.Task = Task.Run(
                () => CloseGraphAfterIdleAsync(repositoryId, work, work.CancellationTokenSource.Token),
                CancellationToken.None);
        }
    }

    private void CancelScheduledGraphClose(Guid repositoryId)
    {
        lock (_cacheWorkLock)
        {
            CancelScheduledGraphCloseCore(repositoryId);
        }
    }

    private void CancelScheduledGraphCloseCore(Guid repositoryId)
    {
        if (_activeGraphCloseWork.Remove(repositoryId, out var work))
        {
            work.CancellationTokenSource.Cancel();
        }
    }

    private async Task CloseGraphAfterIdleAsync(
        Guid repositoryId,
        ActiveGraphCloseWork work,
        CancellationToken cancellationToken)
    {
        var shouldClose = false;
        try
        {
            await Task.Delay(ActiveGraphIdleCloseDelay, cancellationToken).ConfigureAwait(false);
            lock (_cacheWorkLock)
            {
                shouldClose = _activeGraphCloseWork.TryGetValue(repositoryId, out var active)
                    && ReferenceEquals(active, work);
                if (shouldClose)
                {
                    _activeGraphCloseWork.Remove(repositoryId);
                }
            }

            if (shouldClose)
            {
                CloseGraph(repositoryId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            work.Dispose();
        }
    }

    private sealed class ActiveGraphCloseWork : IActiveGraphWork
    {
        public CancellationTokenSource CancellationTokenSource { get; } = new();

        public Task Task { get; set; } = Task.CompletedTask;

        public void Dispose()
        {
            CancellationTokenSource.Dispose();
        }
    }
}
