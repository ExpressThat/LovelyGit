using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

public sealed partial class CommitGraphManager
{
    internal static bool TryGetNextCommit(
        CommitGraphTraversalSession session,
        out GitObjectId hash)
    {
        if (TryDequeueNextCommit(session, out var pending))
        {
            hash = pending.Commit.Hash;
            return true;
        }

        hash = default;
        return false;
    }

    internal static bool TryDequeueNextCommit(
        CommitGraphTraversalSession session,
        out CommitGraphTraversalSession.PendingFrontierCommit pending)
    {
        var hasTip = session.TipFrontier.TryPeek(out _, out var tipPriority);
        var hasActive = session.ActiveFrontier.TryPeek(out _, out var activePriority);
        if (!hasTip && !hasActive)
        {
            pending = default;
            return false;
        }

        var useTip = hasTip
            && (!hasActive || CommitGraphTraversalSession.PriorityComparer.Instance.Compare(
                tipPriority,
                activePriority) <= 0);

        if (useTip && session.TipFrontier.TryDequeue(out var tipCommit, out tipPriority))
        {
            pending = new CommitGraphTraversalSession.PendingFrontierCommit(true, tipCommit, tipPriority);
            return true;
        }

        if (session.ActiveFrontier.TryDequeue(out var activeCommit, out activePriority))
        {
            pending = new CommitGraphTraversalSession.PendingFrontierCommit(
                false,
                activeCommit,
                activePriority);
            return true;
        }

        pending = default;
        return false;
    }

    internal static void RequeueCommit(
        CommitGraphTraversalSession session,
        CommitGraphTraversalSession.PendingFrontierCommit pending)
    {
        if (pending.IsTip)
        {
            session.TipFrontier.Enqueue(pending.Commit, pending.Priority);
        }
        else
        {
            session.ActiveFrontier.Enqueue(pending.Commit, pending.Priority);
        }
    }
}
