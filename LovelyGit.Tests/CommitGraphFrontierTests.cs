using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests;

public sealed class CommitGraphFrontierTests
{
    [Fact]
    public void TryGetNextCommit_DequeuesTipWhenItHasHighestPriority()
    {
        var session = new CommitGraphTraversalSession(Guid.NewGuid());
        var active = Id("a");
        var tip = Id("b");
        session.EnqueueActiveFrontier(active, Priority(active, 1));
        session.EnqueueTipFrontier(tip, Priority(tip, 2));

        var result = CommitGraphManager.TryGetNextCommit(session, out var next);

        Assert.True(result);
        Assert.Equal(tip, next);
    }

    [Fact]
    public void TryGetNextCommit_DequeuesActiveParentWhenItHasHighestPriority()
    {
        var session = new CommitGraphTraversalSession(Guid.NewGuid());
        var active = Id("a");
        var tip = Id("f");
        session.EnqueueActiveFrontier(active, Priority(active, 3));
        session.EnqueueTipFrontier(tip, Priority(tip, 2));

        var result = CommitGraphManager.TryGetNextCommit(session, out var next);

        Assert.True(result);
        Assert.Equal(active, next);
    }

    [Fact]
    public void TryGetNextCommit_DoesNotSuppressTipsWhenManyParentsArePending()
    {
        var session = new CommitGraphTraversalSession(Guid.NewGuid());
        var tip = Id("f");
        foreach (var active in Enumerable.Range(1, 32).Select(index => Id(index.ToString("x"))))
        {
            session.EnqueueActiveFrontier(active, Priority(active, 1));
        }

        session.EnqueueTipFrontier(tip, Priority(tip, 2));

        var result = CommitGraphManager.TryGetNextCommit(session, out var next);

        Assert.True(result);
        Assert.Equal(tip, next);
    }

    [Fact]
    public void FromCommit_UsesCommitterTimeForGraphPriority()
    {
        var id = Id("a");
        var commit = new GitCommit
        {
            Hash = id,
            AuthorUnixSeconds = 1,
            CommitterUnixSeconds = 2,
        };

        var priority = CommitGraphCommitPriority.FromCommit(commit);

        Assert.Equal(2, priority.Seconds);
    }

    private static CommitGraphCommitPriority Priority(GitObjectId id, long seconds)
    {
        return new CommitGraphCommitPriority(seconds, 1, id.Value);
    }

    private static GitObjectId Id(string prefix)
    {
        return GitObjectId.Parse(prefix.PadRight(40, '0'));
    }
}
