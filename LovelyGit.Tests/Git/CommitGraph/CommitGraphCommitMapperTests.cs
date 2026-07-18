using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using System.Text.Json;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitGraphCommitMapperTests
{
    [Fact]
    public void BuildInfo_ReusesEmptyCollectionsForCommitWithoutRefs()
    {
        var info = CommitGraphCommitMapper.BuildInfo(CreateCommit(), remoteRepositoryUrl: null);

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

        var info = CommitGraphCommitMapper.BuildInfo(commit, remoteRepositoryUrl: null);

        Assert.Collection(
            info.Refs,
            reference => Assert.Equal(CommitRefKind.Local, reference.Kind),
            reference => Assert.Equal(CommitRefKind.Remote, reference.Kind));
        Assert.NotSame(CommitGraphEmptyLists.Refs, info.Refs);
    }

    [Fact]
    public void BuildInfo_SerializesNonDefaultSignatureKind()
    {
        var commit = CreateCommit();
        commit.SignatureKind = GitSignatureKind.Ssh;

        var json = JsonSerializer.Serialize(
            CommitGraphCommitMapper.BuildInfo(commit, remoteRepositoryUrl: null));

        Assert.Contains("\"SignatureKind\":\"Ssh\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildInfo_UsesPreparedRepositoryUrlForTags()
    {
        var commit = CreateCommit();
        commit.AddRefs([new GitCommitRef("release/test", GitRefKind.Tag)]);

        var info = CommitGraphCommitMapper.BuildInfo(
            commit,
            "https://github.com/example/repo");

        Assert.Equal(
            "https://github.com/example/repo/releases/tag/release%2Ftest",
            Assert.Single(info.Refs).RemoteUrl);
    }

    [Fact]
    public void CommitGraphRow_SerializesOnlyTrueBooleanFlags()
    {
        var row = new CommitGraphRow();
        var defaults = JsonSerializer.Serialize(row);
        row.IsMergeCommit = true;
        row.IsBranchTip = true;
        var flags = JsonSerializer.Serialize(row);

        Assert.DoesNotContain("IsMergeCommit", defaults, StringComparison.Ordinal);
        Assert.DoesNotContain("IsBranchTip", defaults, StringComparison.Ordinal);
        Assert.Contains("\"IsMergeCommit\":true", flags, StringComparison.Ordinal);
        Assert.Contains("\"IsBranchTip\":true", flags, StringComparison.Ordinal);
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
