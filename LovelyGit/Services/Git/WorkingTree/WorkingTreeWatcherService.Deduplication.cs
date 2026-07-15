using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService
{
    internal int RecentObservedChangeCount
    {
        get { lock (_lock) return _recentObservedChanges.Count; }
    }

    private bool ShouldSuppressObservedChange(WorkingTreeChangedFile change)
    {
        if (!_recentObservedChanges.TryGetValue(change.Path, out var recent))
        {
            return false;
        }

        var elapsed = Stopwatch.GetElapsedTime(recent.Timestamp);
        if (IsDuplicateObservedChange(recent.Status, change.Status, elapsed))
        {
            return true;
        }

        _recentObservedChanges.Remove(change.Path);
        return false;
    }

    internal static bool IsDuplicateObservedChange(
        string recentStatus,
        string currentStatus,
        TimeSpan elapsed) =>
        recentStatus == currentStatus && elapsed <= DuplicateChangeSuppressionWindow
        || recentStatus == "Added" && currentStatus == "Modified"
            && elapsed <= AddedChangeSuppressionWindow;

    private void RememberRecentObservedChange(WorkingTreeChangedFile change, long timestamp)
    {
        var scheduleCleanup = _recentObservedChanges.Count == 0;
        _recentObservedChanges[change.Path] = new RecentObservedChange(change.Status, timestamp);
        if (scheduleCleanup)
        {
            ScheduleRecentObservedChangeCleanup(AddedChangeSuppressionWindow);
        }
    }

    private void ExpireRecentObservedChanges()
    {
        lock (_lock)
        {
            if (_disposed) return;
            var nextCleanup = Timeout.InfiniteTimeSpan;
            foreach (var (path, recent) in _recentObservedChanges)
            {
                var elapsed = Stopwatch.GetElapsedTime(recent.Timestamp);
                if (elapsed >= AddedChangeSuppressionWindow)
                {
                    _recentObservedChanges.Remove(path);
                    continue;
                }

                var remaining = AddedChangeSuppressionWindow - elapsed;
                if (nextCleanup == Timeout.InfiniteTimeSpan || remaining < nextCleanup)
                {
                    nextCleanup = remaining;
                }
            }

            ScheduleRecentObservedChangeCleanup(nextCleanup);
        }
    }

    private void ScheduleRecentObservedChangeCleanup(TimeSpan dueTime)
    {
        if (dueTime != Timeout.InfiniteTimeSpan && dueTime <= TimeSpan.Zero)
        {
            dueTime = TimeSpan.FromMilliseconds(1);
        }

        _recentObservedChangesCleanupTimer.Change(dueTime, Timeout.InfiniteTimeSpan);
    }
}
