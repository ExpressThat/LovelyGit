namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;

internal sealed partial class CommitGraphPageService
{
    internal void ScheduleGraphClose(Guid repositoryId)
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

    internal void CancelScheduledGraphClose(Guid repositoryId)
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
        CommitGraphManager? graphToClose = null;
        try
        {
            await Task.Delay(_activeGraphIdleCloseDelay, cancellationToken).ConfigureAwait(false);
            lock (_cacheWorkLock)
            {
                var shouldClose = _activeGraphCloseWork.TryGetValue(repositoryId, out var active)
                    && ReferenceEquals(active, work);
                if (shouldClose)
                {
                    _activeGraphCloseWork.Remove(repositoryId);
                    _activeGraphs.Remove(repositoryId, out graphToClose);
                }
            }

            graphToClose?.Dispose();
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
