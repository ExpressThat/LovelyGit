using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeStatusScanPolicyTests
{
    [Theory]
    [InlineData(1_000, false)]
    [InlineData(1_001, true)]
    public void ShouldSkipNativeScanBeforeRootTracking_UsesEntryLimit(
        uint trackedEntries,
        bool expected)
    {
        Assert.Equal(
            expected,
            WorkingTreeStatusScanPolicy.ShouldSkipNativeScanBeforeRootTracking(trackedEntries));
    }

}
