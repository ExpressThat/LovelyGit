namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService
{
    private bool _notificationsSuppressed;

    private void ChangeNotificationSuppression(Guid repositoryId, bool suppressed)
    {
        var refresh = false;
        lock (_lock)
        {
            if (_disposed || _activeRepositoryId != repositoryId ||
                _notificationsSuppressed == suppressed)
            {
                return;
            }

            _notificationsSuppressed = suppressed;
            if (suppressed)
            {
                _workTreeDebounceTimer.Change(
                    Timeout.InfiniteTimeSpan,
                    Timeout.InfiniteTimeSpan);
                _pendingObservedChanges.Clear();
                _pendingObservedChangesOverflowed = false;
            }
            SetWatchersEnabled(!suppressed);
            refresh = !suppressed;
        }
        if (refresh) QueueInvalidation();
    }

    private void SetWatchersEnabled(bool enabled)
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = enabled;
        }
    }
}
