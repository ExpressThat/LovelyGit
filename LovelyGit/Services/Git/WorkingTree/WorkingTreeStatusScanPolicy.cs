namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class WorkingTreeStatusScanPolicy
{
    // Above this point Git's optimized index/stat traversal outpaces the
    // managed per-entry scan while preserving the same porcelain contract.
    public const uint MaxTrackedEntriesForNativeDeepUntrackedScan = 1_000;

    public static bool ShouldSkipNativeScanBeforeRootTracking(uint trackedEntryCount)
    {
        return trackedEntryCount > MaxTrackedEntriesForNativeDeepUntrackedScan;
    }

}
