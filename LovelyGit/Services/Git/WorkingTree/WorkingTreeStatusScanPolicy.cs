namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class WorkingTreeStatusScanPolicy
{
    public const int MaxTrackedRootDirectoriesForNativeDeepUntrackedScan = 512;
    public const uint MaxTrackedEntriesForNativeDeepUntrackedScan = 25_000;

    public static bool ShouldUseCompleteFallbackForDeepUntrackedScan(
        int trackedRootDirectoryCount,
        uint trackedEntryCount)
    {
        return trackedRootDirectoryCount > MaxTrackedRootDirectoriesForNativeDeepUntrackedScan
            || trackedEntryCount > MaxTrackedEntriesForNativeDeepUntrackedScan;
    }
}
