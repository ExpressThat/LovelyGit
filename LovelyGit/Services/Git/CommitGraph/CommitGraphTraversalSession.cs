using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal readonly record struct CommitGraphCommitPriority(long Seconds, int SortOrder, string Hash)
{
    public static CommitGraphCommitPriority FromCommit(GitCommit commit, bool prioritize = false)
    {
        return new CommitGraphCommitPriority(
            CommitGraphCommitMapper.GetAuthorUnixTimeSeconds(commit),
            prioritize ? 0 : 1,
            commit.Hash.ToString());
    }
}

internal sealed class CommitGraphTraversalSession
{
    private readonly HashSet<string> _seenHashes = new(StringComparer.Ordinal);

    public CommitGraphTraversalSession(Guid repositoryId)
    {
        RepositoryId = repositoryId;
    }

    public Guid RepositoryId { get; }

    public int Offset { get; private set; }

    public List<string?> ActiveLaneTargets { get; private set; } = new();

    public int MaxLaneCount { get; private set; }

    internal PriorityQueue<FrontierCommit, CommitGraphCommitPriority> Frontier { get; } =
        new(CommitPriorityComparer.Instance);

    public bool MarkSeen(string hash)
    {
        return _seenHashes.Add(hash);
    }

    public void EnqueueFrontier(string hash, CommitGraphCommitPriority priority)
    {
        Frontier.Enqueue(new FrontierCommit(hash, priority.Seconds), priority);
    }

    public void SaveState(int offset, List<string?> activeLaneTargets, int maxLaneCount)
    {
        Offset = offset;
        ActiveLaneTargets = activeLaneTargets;
        MaxLaneCount = maxLaneCount;
    }

    internal readonly record struct FrontierCommit(string Hash, long Seconds);

    private sealed class CommitPriorityComparer : IComparer<CommitGraphCommitPriority>
    {
        public static CommitPriorityComparer Instance { get; } = new();

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
