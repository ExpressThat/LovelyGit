using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal readonly record struct CommitGraphCommitPriority(long Seconds, int SortOrder, string Hash)
{
    public static CommitGraphCommitPriority FromCommit(GitCommit commit, bool prioritize = false)
    {
        return new CommitGraphCommitPriority(
            CommitGraphCommitMapper.GetGraphUnixTimeSeconds(commit),
            prioritize ? 0 : 1,
            commit.Hash.Value);
    }
}

internal sealed class CommitGraphTraversalSession
{
    private readonly HashSet<GitObjectId> _seenHashes = new();
    private readonly HashSet<GitObjectId> _processedHashes = new();

    public CommitGraphTraversalSession(Guid repositoryId)
    {
        RepositoryId = repositoryId;
    }

    public Guid RepositoryId { get; }

    public int Offset { get; private set; }

    public List<GitObjectId?> ActiveLaneTargets { get; private set; } = new();
    public List<int> ActiveLaneColors { get; private set; } = new();

    public int MaxLaneCount { get; private set; }
    public int NextColorIndex { get; private set; }

    public IReadOnlySet<GitObjectId> ProcessedHashes => _processedHashes;

    internal PriorityQueue<FrontierCommit, CommitGraphCommitPriority> ActiveFrontier { get; } =
        new(PriorityComparer.Instance);

    internal PriorityQueue<FrontierCommit, CommitGraphCommitPriority> TipFrontier { get; } =
        new(PriorityComparer.Instance);

    public int PendingCount => ActiveFrontier.Count + TipFrontier.Count;

    public bool MarkSeen(GitObjectId hash)
    {
        return _seenHashes.Add(hash);
    }

    public void MarkProcessed(GitObjectId hash)
    {
        _processedHashes.Add(hash);
    }

    public void EnqueueActiveFrontier(GitObjectId hash, CommitGraphCommitPriority priority)
    {
        ActiveFrontier.Enqueue(new FrontierCommit(hash), priority);
    }

    public void EnqueueTipFrontier(GitObjectId hash, CommitGraphCommitPriority priority)
    {
        TipFrontier.Enqueue(new FrontierCommit(hash), priority);
    }

    public int AllocateColor()
    {
        return NextColorIndex++;
    }

    public void SaveState(
        int offset,
        List<GitObjectId?> activeLaneTargets,
        List<int> activeLaneColors,
        int maxLaneCount)
    {
        Offset = offset;
        ActiveLaneTargets = activeLaneTargets;
        ActiveLaneColors = activeLaneColors;
        MaxLaneCount = maxLaneCount;
    }

    internal readonly record struct FrontierCommit(GitObjectId Hash);

    internal readonly record struct PendingFrontierCommit(
        bool IsTip,
        FrontierCommit Commit,
        CommitGraphCommitPriority Priority);

    internal sealed class PriorityComparer : IComparer<CommitGraphCommitPriority>
    {
        public static PriorityComparer Instance { get; } = new();

        public int Compare(CommitGraphCommitPriority x, CommitGraphCommitPriority y)
        {
            var timeCompare = y.Seconds.CompareTo(x.Seconds);
            if (timeCompare != 0)
            {
                return timeCompare;
            }

            var sortCompare = x.SortOrder.CompareTo(y.SortOrder);
            return sortCompare != 0 ? sortCompare : string.CompareOrdinal(x.Hash, y.Hash);
        }
    }
}
