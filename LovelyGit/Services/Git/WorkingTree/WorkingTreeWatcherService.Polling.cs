using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService : IDisposable
{
    private void StartLargeWorkTreePollingCore()
    {
        _workTreePollCancellation = new CancellationTokenSource();
        var cancellation = _workTreePollCancellation;
        _ = Task.Run(() => PollLargeWorkTreeAsync(cancellation), CancellationToken.None);
    }

    private async Task PollLargeWorkTreeAsync(CancellationTokenSource cancellation)
    {
        RefreshLargeWorkTreeSnapshot(cancellation);
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(LargeWorkTreePollInterval, cancellation.Token).ConfigureAwait(false);
                if (RefreshLargeWorkTreeSnapshot(cancellation))
                {
                    QueueInvalidation();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Trace.TraceWarning("Large work tree polling failed: {0}", exception);
            }
        }
    }

    private bool RefreshLargeWorkTreeSnapshot(CancellationTokenSource cancellation)
    {
        string? workTreeDirectory;
        GitIgnoreMatcher? matcher;
        lock (_lock)
        {
            if (_disposed || !ReferenceEquals(_workTreePollCancellation, cancellation))
            {
                return false;
            }

            workTreeDirectory = _activeWorkTreeDirectory;
            matcher = _ignoreMatcher;
        }

        if (string.IsNullOrEmpty(workTreeDirectory))
        {
            return false;
        }

        var nextSnapshot = ComputeWorkTreeSnapshot(workTreeDirectory, matcher, cancellation.Token);
        lock (_lock)
        {
            if (_disposed || !ReferenceEquals(_workTreePollCancellation, cancellation))
            {
                return false;
            }

            if (_workTreeSnapshot == null)
            {
                _workTreeSnapshot = nextSnapshot;
                return false;
            }

            if (_workTreeSnapshot == nextSnapshot)
            {
                return false;
            }

            _workTreeSnapshot = nextSnapshot;
            return true;
        }
    }
}
