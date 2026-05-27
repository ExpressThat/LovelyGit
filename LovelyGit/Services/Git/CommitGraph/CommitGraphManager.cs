using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

public sealed class CommitGraphManager : IDisposable
{
    private readonly LovelyGitRepository _repository;
    private readonly Guid _repositoryId;
    private CommitGraphTraversalSession? _session;
    private bool _disposed;

    private CommitGraphManager(
        LovelyGitRepository repository,
        Guid repositoryId)
    {
        _repository = repository;
        _repositoryId = repositoryId;
    }

    public int CommitCount => -1;

    public static async Task<CommitGraphOpenResult> TryOpenAsync(
        string gitDirOrWorkTreePath,
        Guid repositoryId,
        CommitGraphRepository commitGraphRepository,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gitDirOrWorkTreePath))
        {
            return new CommitGraphOpenResult(false, null, "Path is null or empty.");
        }

        try
        {
            var repository = await LovelyGitRepository.OpenAsync(gitDirOrWorkTreePath, cancellationToken)
                .ConfigureAwait(false);
            return new CommitGraphOpenResult(
                true,
                new CommitGraphManager(repository, repositoryId),
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
            throw new ObjectDisposedException(nameof(CommitGraphManager));
        }

        if (limit < 0)
        {
            limit = 0;
        }

        var session = await OpenTraversalSessionAsync(cursor, cancellationToken).ConfigureAwait(false);
        var response = await ReadRowsAsync(session, limit, collectRows: true, cancellationToken).ConfigureAwait(false);

        if (!response.HasMore)
        {
            _session = null;
        }

        var nextCursor = new CommitGraphCursorState(response.HasMore ? session.RepositoryId : null, session.Offset);
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

    private async Task<CommitGraphResponse> ReadRowsAsync(
        CommitGraphTraversalSession session,
        int limit,
        bool collectRows,
        CancellationToken cancellationToken)
    {
        var offset = session.Offset;
        var activeLaneTargets = session.ActiveLaneTargets;
        var maxLaneCount = session.MaxLaneCount;
        var rows = collectRows
            ? limit == int.MaxValue ? new List<CommitGraphRow>() : new List<CommitGraphRow>(limit)
            : new List<CommitGraphRow>();
        var rowCount = 0;

        while (rowCount < limit && session.Frontier.TryDequeue(out var nextCommit, out _))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var commit = await TryGetCommitAsync(nextCommit.Hash, cancellationToken).ConfigureAwait(false);
            if (commit == null)
            {
                continue;
            }

            var rowIndex = offset + rowCount;
            var hash = commit.Hash.ToString();
            var parents = commit.ParentHashes.Select(parent => parent.ToString()).ToList();

            foreach (var parentHash in parents)
            {
                if (!session.MarkSeen(parentHash))
                {
                    continue;
                }

                var parent = await TryGetCommitAsync(parentHash, cancellationToken).ConfigureAwait(false);
                if (parent != null)
                {
                    session.EnqueueFrontier(
                        parentHash,
                        CommitGraphCommitPriority.FromCommit(parent));
                }
            }

            var incomingLanes = CommitGraphLaneLayout.FindAllLanesByTarget(activeLaneTargets, hash);
            var activeLanesAbove = collectRows
                ? CommitGraphLaneLayout.GetActiveLanes(activeLaneTargets)
                : null;
            var currentLane = incomingLanes.Count > 0
                ? incomingLanes[0]
                : CommitGraphLaneLayout.AllocateLane(activeLaneTargets);

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
                CommitGraphLaneLayout.SetLaneTarget(activeLaneTargets, currentLane, mainParent);
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
                    var parentLane = CommitGraphLaneLayout.FindLaneByTarget(activeLaneTargets, parent)
                        ?? CommitGraphLaneLayout.AllocateLane(activeLaneTargets);
                    CommitGraphLaneLayout.SetLaneTarget(activeLaneTargets, parentLane, parent);
                    mergeParentLanes ??= new List<int>();
                    mergeParentLanes.Add(parentLane);
                }
            }

            CommitGraphLaneLayout.TrimTrailingEmptyLanes(activeLaneTargets);
            maxLaneCount = Math.Max(maxLaneCount, activeLaneTargets.Count);

            if (collectRows)
            {
                var activeLanesBelow = CommitGraphLaneLayout.GetActiveLanes(activeLaneTargets);
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

                var commitInfo = CommitGraphCommitMapper.BuildInfo(commit, parents);
                rows.Add(new CommitGraphRow
                {
                    Commit = commitInfo,
                    RowIndex = rowIndex,
                    Lane = currentLane,
                    ActiveLanesAbove = activeLanesAbove!,
                    ActiveLanesBelow = activeLanesBelow,
                    EdgesAbove = edgesAbove,
                    EdgesBelow = edgesBelow,
                    IsMergeCommit = parents.Count > 1,
                    IsBranchTip = commitInfo.Branches.Count > 0,
                });
            }

            rowCount++;
        }

        var nextOffset = offset + rowCount;
        var hasMore = session.Frontier.Count > 0;
        session.SaveState(nextOffset, activeLaneTargets, maxLaneCount);

        var response = new CommitGraphResponse
        {
            TotalRows = hasMore ? nextOffset + limit : nextOffset,
            LaneCount = maxLaneCount,
            Rows = rows,
            HasMore = hasMore,
        };

        return response;
    }

    private async Task<CommitGraphTraversalSession> OpenTraversalSessionAsync(
        CommitGraphCursorState cursor,
        CancellationToken cancellationToken)
    {
        if (cursor.RepositoryId == _repositoryId && _session != null)
        {
            return _session;
        }

        _session = await CreateTraversalSessionAsync(cancellationToken).ConfigureAwait(false);
        return _session;
    }

    private async Task<CommitGraphTraversalSession> CreateTraversalSessionAsync(CancellationToken cancellationToken)
    {
        var created = new CommitGraphTraversalSession(_repositoryId);
        foreach (var head in await GetStartingCommitsAsync(cancellationToken).ConfigureAwait(false))
        {
            var hash = head.Hash.ToString();
            if (created.MarkSeen(hash))
            {
                created.EnqueueFrontier(hash, CommitGraphCommitPriority.FromCommit(head));
            }
        }

        created.SaveState(0, new List<string?>(), 0);
        return created;
    }

    private async Task<IReadOnlyList<GitCommit>> GetStartingCommitsAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetStartingCommitsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<GitCommit?> TryGetCommitAsync(string hash, CancellationToken cancellationToken)
    {
        try
        {
            return await _repository.GetCommitAsync(GitObjectId.Parse(hash), cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public static CommitGraphCursorState DecodeCursorState(string? cursor)
    {
        return CommitGraphCursor.Decode(cursor);
    }

    public static string EncodeCursorState(CommitGraphCursorState cursor)
    {
        return CommitGraphCursor.Encode(cursor);
    }
}
