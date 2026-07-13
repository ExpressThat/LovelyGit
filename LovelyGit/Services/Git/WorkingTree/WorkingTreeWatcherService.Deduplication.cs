using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService
{
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
}
