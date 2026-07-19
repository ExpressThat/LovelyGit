using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeWatcherBurstTests
{
    [Fact]
    public void AccumulatorDropsOptimisticPayloadAfterBound()
    {
        var pending = new List<WorkingTreeChangedFile>();
        var overflowed = false;

        for (var index = 0; index < 100_000; index++)
        {
            overflowed = WorkingTreeWatcherService.AccumulatePendingObservedChange(
                pending,
                overflowed,
                new WorkingTreeChangedFile
                {
                    Path = $"file-{index:D6}.txt",
                    Status = "Deleted",
                    Group = WorkingTreeChangeGroup.Unstaged,
                });
        }

        Assert.True(overflowed);
        Assert.Empty(pending);
    }
}
