using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using GitReader;
using GitReader.Collections;
using GitReader.Structures;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

public sealed class CommitGraphNative : IDisposable
{
    private const int CommitMessagePreviewChars = 160;
    private const string LaneSeparator = "\n";

    private readonly StructuredRepository _repository;
    private readonly CommitGraphRepository _commitGraphRepository;
    private readonly string _repositoryId;
    private bool _disposed;

    private CommitGraphNative(
        StructuredRepository repository,
        string repositoryId,
        CommitGraphRepository commitGraphRepository)
    {
        _repository = repository;
        _repositoryId = repositoryId;
        _commitGraphRepository = commitGraphRepository;
    }

    public int CommitCount => -1;

    public static async Task<CommitGraphOpenResult> TryOpenAsync(
        string gitDirOrWorkTreePath,
        string repositoryId,
        CommitGraphRepository commitGraphRepository,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gitDirOrWorkTreePath))
        {
            return new CommitGraphOpenResult(false, null, "Path is null or empty.");
        }

        if (!IsSafeRepositoryId(repositoryId))
        {
            return new CommitGraphOpenResult(false, null, "Repository id is invalid.");
        }

        try
        {
            var repository = await Repository.Factory.OpenStructureAsync(gitDirOrWorkTreePath, cancellationToken)
                .ConfigureAwait(false);
            return new CommitGraphOpenResult(
                true,
                new CommitGraphNative(repository, repositoryId, commitGraphRepository),
                null);
        }
        catch (Exception ex)
        {
            return new CommitGraphOpenResult(false, null, ex.Message);
        }
    }

    public async Task<CommitGraphPageResult> GetCommitGraphPageAsync(
        CommitGraphCursorState cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CommitGraphNative));
        }

        if (limit < 0)
        {
            limit = 0;
        }

        var session = await OpenTraversalSessionAsync(cancellationToken).ConfigureAwait(false);
        var offset = session.Offset;
        var activeLaneTargets = session.ActiveLaneTargets;
        var maxLaneCount = session.MaxLaneCount;
        var rows = new List<CommitGraphRow>(limit);

        while (rows.Count < limit && session.Frontier.TryDequeue(out var nextCommit, out _))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await session.DeleteFrontierAsync(nextCommit.Hash, cancellationToken).ConfigureAwait(false);
            var commit = await TryGetCommitAsync(nextCommit.Hash, cancellationToken).ConfigureAwait(false);
            if (commit == null)
            {
                continue;
            }

            var rowIndex = offset + rows.Count;
            var hash = commit.Hash.ToString();
            var parentCommits = await TryGetParentCommitsAsync(commit, cancellationToken).ConfigureAwait(false);
            var parents = parentCommits.Select(parent => parent.Hash.ToString()).ToList();

            foreach (var parent in parentCommits)
            {
                var parentHash = parent.Hash.ToString();
                if (await session.MarkSeenAsync(parentHash, cancellationToken).ConfigureAwait(false))
                {
                    await session.EnqueueFrontierAsync(
                        parentHash,
                        CommitPriority.FromCommit(parent),
                        cancellationToken).ConfigureAwait(false);
                }
            }

            var incomingLanes = FindAllLanesByTarget(activeLaneTargets, hash);
            var activeLanesAbove = GetActiveLanes(activeLaneTargets);
            var currentLane = incomingLanes.Count > 0 ? incomingLanes[0] : AllocateLane(activeLaneTargets);

            foreach (var lane in incomingLanes)
            {
                activeLaneTargets[lane] = null;
            }

            var mainParent = parents.Count > 0 ? parents[0] : null;
            List<string>? mergeParents = null;
            if (parents.Count > 1)
            {
                mergeParents = parents.GetRange(1, parents.Count - 1);
            }

            if (!string.IsNullOrEmpty(mainParent))
            {
                SetLaneTarget(activeLaneTargets, currentLane, mainParent);
            }
            else if (currentLane < activeLaneTargets.Count)
            {
                activeLaneTargets[currentLane] = null;
            }

            List<int>? mergeParentLanes = null;
            if (mergeParents != null)
            {
                foreach (var parent in mergeParents)
                {
                    var parentLane = FindLaneByTarget(activeLaneTargets, parent) ?? AllocateLane(activeLaneTargets);
                    SetLaneTarget(activeLaneTargets, parentLane, parent);
                    mergeParentLanes ??= new List<int>();
                    mergeParentLanes.Add(parentLane);
                }
            }

            TrimTrailingEmptyLanes(activeLaneTargets);
            maxLaneCount = Math.Max(maxLaneCount, activeLaneTargets.Count);

            var activeLanesBelow = GetActiveLanes(activeLaneTargets);
            var edgesAbove = incomingLanes
                .Select(lane => new CommitLaneEdge
                {
                    FromLane = lane,
                    ToLane = currentLane,
                    Kind = lane == currentLane ? "straight" : "merge_in",
                })
                .ToList();

            var edgesBelow = new List<CommitLaneEdge>();
            if (!string.IsNullOrEmpty(mainParent))
            {
                edgesBelow.Add(new CommitLaneEdge
                {
                    FromLane = currentLane,
                    ToLane = currentLane,
                    Kind = "straight",
                });
            }

            if (mergeParentLanes != null)
            {
                foreach (var parentLane in mergeParentLanes)
                {
                    edgesBelow.Add(new CommitLaneEdge
                    {
                        FromLane = currentLane,
                        ToLane = parentLane,
                        Kind = "merge_in",
                    });
                }
            }

            var commitInfo = BuildCommitInfo(commit, parents);
            rows.Add(new CommitGraphRow
            {
                Commit = commitInfo,
                RowIndex = rowIndex,
                Lane = currentLane,
                ActiveLanes = activeLanesBelow.ToList(),
                ActiveLanesAbove = activeLanesAbove,
                ActiveLanesBelow = activeLanesBelow,
                EdgesAbove = edgesAbove,
                EdgesBelow = edgesBelow,
                IsMergeCommit = parents.Count > 1,
                IsBranchTip = commitInfo.Branches.Count > 0,
            });
        }

        var nextOffset = offset + rows.Count;
        var hasMore = session.Frontier.Count > 0;
        await session.SaveStateAsync(nextOffset, activeLaneTargets, maxLaneCount, cancellationToken)
            .ConfigureAwait(false);

        var response = new CommitGraphResponse
        {
            TotalRows = hasMore ? nextOffset + limit : nextOffset,
            LaneCount = maxLaneCount,
            Rows = rows,
            HasMore = hasMore,
        };

        if (!hasMore)
        {
            await session.DeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        var nextCursor = new CommitGraphCursorState(hasMore ? session.RepositoryId : null, nextOffset);
        return new CommitGraphPageResult(response, nextCursor);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _repository.Dispose();
        _disposed = true;
    }

    private async Task<TraversalSession> OpenTraversalSessionAsync(CancellationToken cancellationToken)
    {
        var existing = await TraversalSession.TryOpenAsync(
                _commitGraphRepository,
                _repositoryId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing != null)
        {
            return existing;
        }

        var created = await TraversalSession.CreateAsync(_commitGraphRepository, _repositoryId, cancellationToken).ConfigureAwait(false);
        foreach (var head in await GetStartingCommitsAsync(cancellationToken).ConfigureAwait(false))
        {
            var hash = head.Hash.ToString();
            if (await created.MarkSeenAsync(hash, cancellationToken).ConfigureAwait(false))
            {
                await created.EnqueueFrontierAsync(hash, CommitPriority.FromCommit(head), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        await created.SaveStateAsync(0, new List<string?>(), 0, cancellationToken).ConfigureAwait(false);
        return created;
    }

    private async Task<List<Commit>> GetStartingCommitsAsync(CancellationToken cancellationToken)
    {
        var starts = new List<Commit>();

        foreach (var branch in _repository.Branches.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await TryGetBranchHeadAsync(branch, cancellationToken).ConfigureAwait(false) is { } commit)
            {
                starts.Add(commit);
            }
        }

        foreach (var branchGroup in _repository.BranchesAll.Values)
        {
            foreach (var branch in branchGroup)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await TryGetBranchHeadAsync(branch, cancellationToken).ConfigureAwait(false) is { } commit)
                {
                    starts.Add(commit);
                }
            }
        }

        foreach (var tag in _repository.Tags.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                starts.Add(await tag.GetCommitAsync(cancellationToken).ConfigureAwait(false));
            }
            catch
            {
                // Non-commit tags are not graph starting points.
            }
        }

        if (_repository.Head is { } head)
        {
            if (await TryGetBranchHeadAsync(head, cancellationToken).ConfigureAwait(false) is { } commit)
            {
                starts.Add(commit);
            }
        }

        return starts;
    }

    private async Task<Commit?> TryGetCommitAsync(string hash, CancellationToken cancellationToken)
    {
        try
        {
            return await _repository.GetCommitAsync(Hash.Parse(hash), cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static async Task<Commit?> TryGetBranchHeadAsync(Branch branch, CancellationToken cancellationToken)
    {
        try
        {
            return await branch.GetHeadCommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static async Task<IReadOnlyList<Commit>> TryGetParentCommitsAsync(
        Commit commit,
        CancellationToken cancellationToken)
    {
        try
        {
            return await commit.GetParentCommitsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return Array.Empty<Commit>();
        }
    }

    private static CommitInfo BuildCommitInfo(Commit commit, List<string> parents)
    {
        var message = commit.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            message = commit.Body?.Trim() ?? string.Empty;
        }

        var (authorName, authorEmail, authorDate) = CrackSignature(commit.Author);

        return new CommitInfo
        {
            Hash = commit.Hash.ToString(),
            Parents = parents,
            Author = string.IsNullOrWhiteSpace(authorName) ? "unknown" : authorName,
            Email = authorEmail,
            Date = authorDate.ToUnixTimeSeconds(),
            Message = TruncateMessage(message),
            Branches = commit.Branches.Select(branch => branch.Name).ToList(),
            Tags = commit.Tags.Select(tag => tag.Name).ToList(),
            Stats = null,
        };
    }

    private static (string Name, string Email, DateTimeOffset Date) CrackSignature(Signature signature)
    {
        var raw = signature.RawFormat;
        var emailStart = raw.LastIndexOf('<');
        var emailEnd = raw.LastIndexOf('>');
        if (emailStart < 0 || emailEnd <= emailStart)
        {
            return (signature.ToString(), string.Empty, DateTimeOffset.UnixEpoch);
        }

        var name = raw[..emailStart].Trim();
        var email = raw[(emailStart + 1)..emailEnd].Trim();
        var remainder = raw[(emailEnd + 1)..].Trim();
        var firstSpace = remainder.IndexOf(' ');
        var timestampText = firstSpace >= 0 ? remainder[..firstSpace] : remainder;

        return long.TryParse(timestampText, out var seconds)
            ? (name, email, DateTimeOffset.FromUnixTimeSeconds(seconds))
            : (name, email, DateTimeOffset.UnixEpoch);
    }

    private static string TruncateMessage(string value)
    {
        return value.Length <= CommitMessagePreviewChars ? value : value.Substring(0, CommitMessagePreviewChars);
    }

    private static List<int> FindAllLanesByTarget(List<string?> activeLaneTargets, string target)
    {
        var lanes = new List<int>();
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] == target)
            {
                lanes.Add(i);
            }
        }
        return lanes;
    }

    private static int? FindLaneByTarget(List<string?> activeLaneTargets, string target)
    {
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] == target)
            {
                return i;
            }
        }
        return null;
    }

    private static int AllocateLane(List<string?> activeLaneTargets)
    {
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] == null)
            {
                return i;
            }
        }

        activeLaneTargets.Add(null);
        return activeLaneTargets.Count - 1;
    }

    private static void SetLaneTarget(List<string?> activeLaneTargets, int lane, string target)
    {
        while (lane >= activeLaneTargets.Count)
        {
            activeLaneTargets.Add(null);
        }
        activeLaneTargets[lane] = target;
    }

    private static void TrimTrailingEmptyLanes(List<string?> activeLaneTargets)
    {
        while (activeLaneTargets.Count > 0 && activeLaneTargets[^1] == null)
        {
            activeLaneTargets.RemoveAt(activeLaneTargets.Count - 1);
        }
    }

    private static List<int> GetActiveLanes(List<string?> activeLaneTargets)
    {
        var lanes = new List<int>();
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] != null)
            {
                lanes.Add(i);
            }
        }
        return lanes;
    }

    private static bool IsSafeRepositoryId(string repositoryId)
    {
        return repositoryId.Length == 32 && repositoryId.All(Uri.IsHexDigit);
    }

    private readonly record struct CommitPriority(long Seconds, string Hash)
    {
        public static CommitPriority FromCommit(Commit commit)
        {
            var (_, _, date) = CrackSignature(commit.Author);
            return new CommitPriority(date.ToUnixTimeSeconds(), commit.Hash.ToString());
        }
    }

    private sealed class CommitPriorityComparer : IComparer<CommitPriority>
    {
        public static CommitPriorityComparer Instance { get; } = new();

        public int Compare(CommitPriority x, CommitPriority y)
        {
            var timeCompare = y.Seconds.CompareTo(x.Seconds);
            return timeCompare != 0 ? timeCompare : string.CompareOrdinal(x.Hash, y.Hash);
        }
    }

    private readonly record struct FrontierCommit(string Hash, long Seconds);

    private sealed class TraversalSession
    {
        private readonly CommitGraphRepository _repository;

        private TraversalSession(
            string repositoryId,
            CommitGraphRepository repository,
            int offset,
            List<string?> activeLaneTargets,
            int maxLaneCount,
            PriorityQueue<FrontierCommit, CommitPriority> frontier)
        {
            RepositoryId = repositoryId;
            _repository = repository;
            Offset = offset;
            ActiveLaneTargets = activeLaneTargets;
            MaxLaneCount = maxLaneCount;
            Frontier = frontier;
        }

        public string RepositoryId { get; }

        public int Offset { get; private set; }

        public List<string?> ActiveLaneTargets { get; private set; }

        public int MaxLaneCount { get; private set; }

        public PriorityQueue<FrontierCommit, CommitPriority> Frontier { get; }

        public static async Task<TraversalSession> CreateAsync(
            CommitGraphRepository repository,
            string repositoryId,
            CancellationToken cancellationToken)
        {
            return new TraversalSession(
                repositoryId,
                repository,
                0,
                new List<string?>(),
                0,
                await LoadFrontierAsync(repository, repositoryId, cancellationToken).ConfigureAwait(false));
        }

        public static async Task<TraversalSession?> TryOpenAsync(
            CommitGraphRepository repository,
            string repositoryId,
            CancellationToken cancellationToken)
        {
            if (!IsSafeRepositoryId(repositoryId))
            {
                return null;
            }

            var state = await repository.GetRepositoryStateAsync(repositoryId, cancellationToken).ConfigureAwait(false);
            if (state == null)
            {
                return null;
            }

            var offset = state.Offset;
            var maxLaneCount = state.MaxLaneCount;
            var lanes = DecodeLaneTargets(state.Lanes);
            var frontier = await LoadFrontierAsync(repository, repositoryId, cancellationToken).ConfigureAwait(false);

            return new TraversalSession(repositoryId, repository, offset, lanes, maxLaneCount, frontier);
        }

        public async Task<bool> MarkSeenAsync(string hash, CancellationToken cancellationToken)
        {
            if (await _repository.HasSeenAsync(RepositoryId, hash, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            await _repository.AddSeenAsync(RepositoryId, hash, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task EnqueueFrontierAsync(
            string hash,
            CommitPriority priority,
            CancellationToken cancellationToken)
        {
            Frontier.Enqueue(new FrontierCommit(hash, priority.Seconds), priority);
            await _repository.AddFrontierAsync(RepositoryId, hash, priority.Seconds, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeleteFrontierAsync(string hash, CancellationToken cancellationToken)
        {
            await _repository.DeleteFrontierAsync(RepositoryId, hash, cancellationToken).ConfigureAwait(false);
        }

        public async Task SaveStateAsync(
            int offset,
            List<string?> activeLaneTargets,
            int maxLaneCount,
            CancellationToken cancellationToken)
        {
            Offset = offset;
            ActiveLaneTargets = activeLaneTargets;
            MaxLaneCount = maxLaneCount;

            await _repository.SaveRepositoryStateAsync(
                RepositoryId,
                offset,
                maxLaneCount,
                EncodeLaneTargets(activeLaneTargets),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            await _repository.DeleteTraversalEntriesAsync(RepositoryId, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<PriorityQueue<FrontierCommit, CommitPriority>> LoadFrontierAsync(
            CommitGraphRepository repository,
            string repositoryId,
            CancellationToken cancellationToken)
        {
            var queue = new PriorityQueue<FrontierCommit, CommitPriority>(CommitPriorityComparer.Instance);
            await foreach (var entry in repository.GetFrontierAsync(repositoryId, cancellationToken).ConfigureAwait(false))
            {
                if (!string.IsNullOrWhiteSpace(entry.Hash))
                {
                    queue.Enqueue(
                        new FrontierCommit(entry.Hash, entry.Seconds),
                        new CommitPriority(entry.Seconds, entry.Hash));
                }
            }

            return queue;
        }

        private static string EncodeLaneTargets(List<string?> laneTargets)
        {
            return string.Join(LaneSeparator, laneTargets.Select(target => target ?? string.Empty));
        }

        private static List<string?> DecodeLaneTargets(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new List<string?>();
            }

            return value.Split(LaneSeparator).Select(target => string.IsNullOrEmpty(target) ? null : target).ToList();
        }

    }
}

public readonly record struct CommitGraphOpenResult(bool Success, CommitGraphNative? Graph, string? Error);
public readonly record struct CommitGraphCursorState(string? RepositoryId, int Offset);
public readonly record struct CommitGraphPageResult(CommitGraphResponse Response, CommitGraphCursorState NextCursor);
