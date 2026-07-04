using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests;

public sealed class CommitGraphRowBuilderTests
{
    [Fact]
    public void Build_ReusesExistingLaneForFirstParent()
    {
        var parent = Id("a");
        var commit = new GitCommit
        {
            Hash = Id("b"),
            Subject = "child",
        };
        commit.AddParentHash(parent);
        var activeLanes = new List<GitObjectId?> { parent, commit.Hash };
        var maxLaneCount = activeLanes.Count;

        var row = Build(
            commit,
            new CommitGraphParentSet(commit, StashMainOnly: false),
            rowIndex: 1,
            activeLanes,
            ref maxLaneCount);

        Assert.Equal([parent], activeLanes);
        Assert.Equal([0], row.ActiveLanesBelow);
        var edge = Assert.Single(row.EdgesBelow);
        Assert.Equal(1, edge.FromLane);
        Assert.Equal(0, edge.ToLane);
    }

    [Fact]
    public void Build_PreservesDuplicateTargetsUntilSharedCommit()
    {
        var parent = Id("a");
        var commit = new GitCommit
        {
            Hash = Id("b"),
            Subject = "child",
        };
        commit.AddParentHash(parent);
        var activeLanes = new List<GitObjectId?> { parent, commit.Hash, parent };
        var maxLaneCount = activeLanes.Count;

        var row = Build(
            commit,
            new CommitGraphParentSet(commit, StashMainOnly: false),
            rowIndex: 1,
            activeLanes,
            ref maxLaneCount);

        Assert.Equal([parent, parent], activeLanes);
        Assert.Equal([0, 1], row.ActiveLanesBelow);
        Assert.Contains(row.EdgesBelow, edge => edge.FromLane == 1 && edge.ToLane == 0);
        Assert.Contains(row.EdgesBelow, edge => edge.FromLane == 2 && edge.ToLane == 1);
    }

    [Fact]
    public void Build_MergesDuplicateTargetsAtSharedCommit()
    {
        var parent = Id("a");
        var root = Id("b");
        var commit = new GitCommit
        {
            Hash = parent,
            Subject = "shared",
        };
        commit.AddParentHash(root);
        var activeLanes = new List<GitObjectId?> { parent, parent };
        var maxLaneCount = activeLanes.Count;

        var row = Build(
            commit,
            new CommitGraphParentSet(commit, StashMainOnly: false),
            rowIndex: 2,
            activeLanes,
            ref maxLaneCount);

        Assert.Equal([root], activeLanes);
        Assert.Equal(0, row.Lane);
        Assert.Contains(row.EdgesAbove, edge => edge.FromLane == 0 && edge.ToLane == 0);
        Assert.Contains(row.EdgesAbove, edge => edge.FromLane == 1 && edge.ToLane == 0);
    }

    [Fact]
    public void Build_DropsLaneWhenParentWasAlreadyDrawn()
    {
        var parent = Id("a");
        var commit = new GitCommit
        {
            Hash = Id("b"),
            Subject = "late child",
        };
        commit.AddParentHash(parent);
        var activeLanes = new List<GitObjectId?> { commit.Hash };
        var maxLaneCount = activeLanes.Count;

        var row = Build(
            commit,
            new CommitGraphParentSet(commit, false, new HashSet<GitObjectId> { parent }),
            rowIndex: 3,
            activeLanes,
            ref maxLaneCount);

        Assert.Empty(activeLanes);
        Assert.Empty(row.ActiveLanesBelow);
        Assert.Empty(row.EdgesBelow);
    }

    [Fact]
    public void Build_AvoidsNeighborColorsForNewFlow()
    {
        var commit = new GitCommit
        {
            Hash = Id("c"),
            Subject = "new flow",
        };
        var activeLanes = new List<GitObjectId?> { Id("a"), null, Id("b") };
        var activeColors = new List<int> { 2, -1, 3 };
        var maxLaneCount = activeLanes.Count;

        var row = CommitGraphRowBuilder.Build(
            commit,
            new CommitGraphParentSet(commit, StashMainOnly: false),
            rowIndex: 1,
            activeLanes,
            activeColors,
            () => 2,
            ref maxLaneCount);

        Assert.Equal(4, row.ColorIndex);
    }

    [Fact]
    public void Build_KeepsFlowColorWhenLaneCompacts()
    {
        var parent = Id("a");
        var commit = new GitCommit
        {
            Hash = Id("b"),
            Subject = "child",
        };
        commit.AddParentHash(parent);
        var other = Id("c");
        var activeLanes = new List<GitObjectId?> { parent, commit.Hash, other };
        var activeColors = new List<int> { 0, 4, 1 };
        var maxLaneCount = activeLanes.Count;

        var row = CommitGraphRowBuilder.Build(
            commit,
            new CommitGraphParentSet(commit, StashMainOnly: false),
            rowIndex: 1,
            activeLanes,
            activeColors,
            () => 2,
            ref maxLaneCount);

        Assert.Equal(4, row.ColorIndex);
        Assert.Contains(row.EdgesBelow, edge => edge.FromLane == 1 && edge.ColorIndex == 4);
        Assert.Equal([0, 1], activeColors);
    }

    [Fact]
    public void Build_RecolorsCompactedLaneAwayFromMatchingNeighbor()
    {
        var parent = Id("a");
        var commit = new GitCommit
        {
            Hash = Id("b"),
            Subject = "child",
        };
        commit.AddParentHash(parent);
        var other = Id("c");
        var activeLanes = new List<GitObjectId?> { parent, commit.Hash, other };
        var activeColors = new List<int> { 0, 4, 0 };
        var maxLaneCount = activeLanes.Count;

        CommitGraphRowBuilder.Build(
            commit,
            new CommitGraphParentSet(commit, StashMainOnly: false),
            rowIndex: 1,
            activeLanes,
            activeColors,
            () => 2,
            ref maxLaneCount);

        Assert.Equal([0, 1], activeColors);
    }

    private static GitObjectId Id(string prefix)
    {
        return GitObjectId.Parse(prefix.PadRight(40, '0'));
    }

    private static ExpressThat.LovelyGit.Services.Git.CommitGraph.Models.CommitGraphRow Build(
        GitCommit commit,
        CommitGraphParentSet parents,
        int rowIndex,
        List<GitObjectId?> activeLanes,
        ref int maxLaneCount)
    {
        var colors = Enumerable.Repeat(0, activeLanes.Count).ToList();
        var nextColor = 1;
        return CommitGraphRowBuilder.Build(
            commit,
            parents,
            rowIndex,
            activeLanes,
            colors,
            () => nextColor++,
            ref maxLaneCount);
    }
}
