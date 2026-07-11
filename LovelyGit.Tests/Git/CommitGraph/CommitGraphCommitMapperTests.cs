using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitGraphCommitMapperTests
{
    [Fact]
    public void BuildInfo_ReusesEmptyCollectionsForCommitWithoutRefs()
    {
        var info = CommitGraphCommitMapper.BuildInfo(CreateCommit(), [], remoteUrl: null);

        Assert.Same(CommitGraphEmptyLists.Strings, info.Branches);
        Assert.Same(CommitGraphEmptyLists.Strings, info.Tags);
        Assert.Same(CommitGraphEmptyLists.Refs, info.Refs);
    }

    [Fact]
    public void BuildInfo_CopiesAndMapsPopulatedRefs()
    {
        var commit = CreateCommit();
        commit.AddBranches(["main"]);
        commit.AddTags(["v1.0"]);
        commit.AddRefs(
        [
            new GitCommitRef("main", GitRefKind.Head),
            new GitCommitRef("origin/main", GitRefKind.Remote),
        ]);

        var info = CommitGraphCommitMapper.BuildInfo(commit, [], remoteUrl: null);

        Assert.Equal(["main"], info.Branches);
        Assert.Equal(["v1.0"], info.Tags);
        Assert.Collection(
            info.Refs,
            reference => Assert.Equal(CommitRefKind.Local, reference.Kind),
            reference => Assert.Equal(CommitRefKind.Remote, reference.Kind));
        Assert.NotSame(CommitGraphEmptyLists.Strings, info.Branches);
        Assert.NotSame(CommitGraphEmptyLists.Refs, info.Refs);
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
