using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests;

public sealed class CommitGraphLaneLayoutTests
{
    [Fact]
    public void AllocateLane_ReusesEmptyLaneBeforeAddingNewLane()
    {
        var activeLanes = new List<GitObjectId?> { Id("a"), null, Id("c") };

        var lane = CommitGraphLaneLayout.AllocateLane(activeLanes);

        Assert.Equal(1, lane);
        Assert.Equal(3, activeLanes.Count);
    }

    [Fact]
    public void TrimTrailingEmptyLanes_KeepsInteriorEmptyLanes()
    {
        var activeLanes = new List<GitObjectId?> { Id("a"), null, null };

        CommitGraphLaneLayout.TrimTrailingEmptyLanes(activeLanes);

        Assert.Equal([Id("a")], activeLanes);
    }

    private static GitObjectId Id(string prefix)
    {
        return GitObjectId.Parse(prefix.PadRight(40, '0'));
    }
}
