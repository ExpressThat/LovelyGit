using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitGraphCommitMapperTests
{
    [Fact]
    public void BuildInfo_ReusesEmptyCollectionsForCommitWithoutRefs()
    {
        var info = CommitGraphCommitMapper.BuildInfo(CreateCommit(), remoteUrl: null);

        Assert.Same(CommitGraphEmptyLists.Refs, info.Refs);
    }

    [Fact]
    public void BuildInfo_CopiesAndMapsPopulatedRefs()
    {
        var commit = CreateCommit();
        commit.AddRefs(
        [
            new GitCommitRef("main", GitRefKind.Head),
            new GitCommitRef("origin/main", GitRefKind.Remote),
        ]);

        var info = CommitGraphCommitMapper.BuildInfo(commit, remoteUrl: null);

        Assert.Collection(
            info.Refs,
            reference => Assert.Equal(CommitRefKind.Local, reference.Kind),
            reference => Assert.Equal(CommitRefKind.Remote, reference.Kind));
        Assert.NotSame(CommitGraphEmptyLists.Refs, info.Refs);
    }

    [Fact]
    public void BuildRefNames_DerivesDetailsListsFromTypedRefs()
    {
        var commit = CreateCommit();
        commit.AddRefs(
        [
            new GitCommitRef("main", GitRefKind.Head),
            new GitCommitRef("origin/main", GitRefKind.Remote),
            new GitCommitRef("v1.0", GitRefKind.Tag),
            new GitCommitRef("stash", GitRefKind.Stash),
        ]);

        Assert.Equal(
            ["main", "origin/main"],
            CommitDetailsBuilder.BuildRefNames(commit, includeBranches: true));
        Assert.Equal(
            ["v1.0"],
            CommitDetailsBuilder.BuildRefNames(commit, includeBranches: false));
    }

    private static GitCommit CreateCommit()
    {
        return new GitCommit
        {
            Hash = GitObjectId.Parse(new string('a', 40)),
            Subject = "Subject",
        };
    }
}
