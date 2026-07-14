namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class WorkingTreeStatusScanPolicy
{
    public const uint MaxTrackedEntriesForNativeDeepUntrackedScan = 25_000;

    public static bool ShouldSkipNativeScanBeforeRootTracking(uint trackedEntryCount)
    {
        return trackedEntryCount > MaxTrackedEntriesForNativeDeepUntrackedScan;
    }

}
