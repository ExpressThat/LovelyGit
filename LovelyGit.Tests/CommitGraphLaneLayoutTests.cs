using ExpressThat.LovelyGit.Services.Git.CommitGraph;

namespace LovelyGit.Tests;

public sealed class CommitGraphLaneLayoutTests
{
    [Fact]
    public void AllocateLane_ReusesFirstEmptyLane()
    {
        var lanes = new List<string?> { "a", null, "c" };

        var lane = CommitGraphLaneLayout.AllocateLane(lanes);

        Assert.Equal(1, lane);
        Assert.Equal(["a", null, "c"], lanes);
    }

    [Fact]
    public void AllocateLane_AppendsWhenNoEmptyLaneExists()
    {
        var lanes = new List<string?> { "a", "b" };

        var lane = CommitGraphLaneLayout.AllocateLane(lanes);

        Assert.Equal(2, lane);
        Assert.Equal(["a", "b", null], lanes);
    }

    [Fact]
    public void SetLaneTarget_ExpandsLaneList()
    {
        var lanes = new List<string?> { "a" };

        CommitGraphLaneLayout.SetLaneTarget(lanes, 3, "d");

        Assert.Equal(["a", null, null, "d"], lanes);
    }

    [Fact]
    public void FindLaneByTarget_ReturnsFirstMatchingLane()
    {
        var lanes = new List<string?> { "a", "b", "a" };

        var lane = CommitGraphLaneLayout.FindLaneByTarget(lanes, "a");

        Assert.Equal(0, lane);
    }

    [Fact]
    public void FindAllLanesByTarget_ReturnsEveryMatchingLane()
    {
        var lanes = new List<string?> { "a", "b", "a", null };

        var matches = CommitGraphLaneLayout.FindAllLanesByTarget(lanes, "a");

        Assert.Equal([0, 2], matches);
    }

    [Fact]
    public void TrimTrailingEmptyLanes_RemovesOnlyTrailingNulls()
    {
        var lanes = new List<string?> { "a", null, "c", null, null };

        CommitGraphLaneLayout.TrimTrailingEmptyLanes(lanes);

        Assert.Equal(["a", null, "c"], lanes);
    }

    [Fact]
    public void GetActiveLanes_ReturnsNonEmptyLaneIndexes()
    {
        var lanes = new List<string?> { null, "b", null, "d" };

        var active = CommitGraphLaneLayout.GetActiveLanes(lanes);

        Assert.Equal([1, 3], active);
    }
}
