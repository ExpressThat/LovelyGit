using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreePreliminarySummaryTests
{
    [Theory]
    [InlineData(WorkingTreeStatusScanPolicy.MaxTrackedEntriesForNativeDeepUntrackedScan, true)]
    [InlineData(WorkingTreeStatusScanPolicy.MaxTrackedEntriesForNativeDeepUntrackedScan + 1, false)]
    public async Task GetSummaryAsync_BoundsBackgroundFileListPreloading(
        uint entryCount,
        bool expectedPreload)
    {
        using var directory = TemporaryDirectory.Create("lovelygit-preload-policy-");
        var gitDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, ".git"));
        SyntheticGitIndexWriter.WriteVersion2(
            Path.Combine(gitDirectory.FullName, "index"),
            checked((int)entryCount));

        var summary = await new WorkingTreePreliminarySummaryService().GetSummaryAsync(
            directory.Path,
            gitDirectory.FullName,
            CancellationToken.None);

        Assert.False(summary.IsComplete);
        Assert.Equal(expectedPreload, summary.ShouldPreloadChanges);
    }
}
