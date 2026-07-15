using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.BranchComparison;

internal static partial class NativeBranchComparisonReader
{
    private static async Task<HistoryPaintResult> PaintHistoryAsync(
        LovelyGitRepository repository,
        GitObjectId current,
        GitObjectId target,
        int maximumNodes,
        CancellationToken cancellationToken)
    {
        if (current == target)
        {
            return new HistoryPaintResult([], [], current, false);
        }

        var nodes = new Dictionary<GitObjectId, PaintNode>();
        var commonAncestors = new HashSet<GitObjectId>();
        var pending = new Queue<GitObjectId>();
        Enqueue(nodes, commonAncestors, pending, current, Reachability.Current);
        Enqueue(nodes, commonAncestors, pending, target, Reachability.Target);
        var isPartial = false;
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hash = pending.Dequeue();
            var node = nodes[hash];
            node.IsQueued = false;
            var delta = node.Reachability & ~node.Propagated;
            if (delta == Reachability.None) continue;

            if (node.Header == null)
            {
                if (nodes.Count >= maximumNodes)
                {
                    isPartial = true;
                    break;
                }

                node.Header = await repository.GetCommitAncestryHeaderAsync(hash, cancellationToken)
                    .ConfigureAwait(false);
            }

            node.Propagated |= delta;
            if (node.Header is not { } header) continue;
            for (var index = 0; index < header.ParentHashCount; index++)
            {
                var parent = header.GetParentHash(index);
                if (node.Reachability == Reachability.Both)
                {
                    commonAncestors.Add(parent);
                    if (!nodes.ContainsKey(parent)) continue;
                }

                Enqueue(nodes, commonAncestors, pending, parent, delta);
            }
        }

        var ahead = Select(nodes, Reachability.Current);
        var behind = Select(nodes, Reachability.Target);
        var mergeBase = nodes
            .Where(entry => entry.Value.IsCommonBoundary && entry.Value.Header != null)
            .OrderByDescending(entry => entry.Value.Header!.Value.CommitUnixSeconds)
            .Select(entry => (GitObjectId?)entry.Key)
            .FirstOrDefault();
        return new HistoryPaintResult(ahead, behind, mergeBase, isPartial);
    }

    private static List<PaintedCommit> Select(
        Dictionary<GitObjectId, PaintNode> nodes,
        Reachability side) => nodes
        .Where(entry => entry.Value.Reachability == side && entry.Value.Header != null)
        .Select(entry => new PaintedCommit(entry.Key, entry.Value.Header!.Value.CommitUnixSeconds))
        .OrderByDescending(commit => commit.CommitUnixSeconds)
        .ToList();

    private static void Enqueue(
        Dictionary<GitObjectId, PaintNode> nodes,
        HashSet<GitObjectId> commonAncestors,
        Queue<GitObjectId> pending,
        GitObjectId hash,
        Reachability reachability)
    {
        if (!nodes.TryGetValue(hash, out var node))
        {
            node = new PaintNode();
            nodes.Add(hash, node);
        }

        var previous = node.Reachability;
        var inheritedCommon = commonAncestors.Contains(hash);
        node.Reachability |= inheritedCommon ? Reachability.Both : reachability;
        if (!inheritedCommon && previous != Reachability.Both && node.Reachability == Reachability.Both)
        {
            node.IsCommonBoundary = true;
        }
        if (node.IsQueued || (node.Reachability & ~node.Propagated) == Reachability.None) return;
        node.IsQueued = true;
        pending.Enqueue(hash);
    }

    [Flags]
    private enum Reachability : byte { None = 0, Current = 1, Target = 2, Both = 3 }
    private sealed class PaintNode
    {
        public Reachability Reachability { get; set; }
        public Reachability Propagated { get; set; }
        public GitCommitAncestryHeader? Header { get; set; }
        public bool IsQueued { get; set; }
        public bool IsCommonBoundary { get; set; }
    }

    private sealed record HistoryPaintResult(
        List<PaintedCommit> Ahead,
        List<PaintedCommit> Behind,
        GitObjectId? MergeBaseHash,
        bool IsPartial);
    private readonly record struct PaintedCommit(GitObjectId Hash, long CommitUnixSeconds);
}
