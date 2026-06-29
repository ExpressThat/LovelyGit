using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeStatusScanPolicyTests
{
    [Fact]
    public void ShouldUseCompleteFallbackForDeepUntrackedScan_AllowsNormalRepos()
    {
        var shouldFallback =
            WorkingTreeStatusScanPolicy.ShouldUseCompleteFallbackForDeepUntrackedScan(
                20,
                1_000);

        Assert.False(shouldFallback);
    }

    [Fact]
    public void ShouldUseCompleteFallbackForDeepUntrackedScan_SkipsLargeRootScans()
    {
        var shouldFallback =
            WorkingTreeStatusScanPolicy.ShouldUseCompleteFallbackForDeepUntrackedScan(
                WorkingTreeStatusScanPolicy.MaxTrackedRootDirectoriesForNativeDeepUntrackedScan + 1,
                1_000);

        Assert.True(shouldFallback);
    }

    [Fact]
    public void ShouldUseCompleteFallbackForDeepUntrackedScan_SkipsHugeIndexes()
    {
        var shouldFallback =
            WorkingTreeStatusScanPolicy.ShouldUseCompleteFallbackForDeepUntrackedScan(
                20,
                WorkingTreeStatusScanPolicy.MaxTrackedEntriesForNativeDeepUntrackedScan + 1);

        Assert.True(shouldFallback);
    }
}
