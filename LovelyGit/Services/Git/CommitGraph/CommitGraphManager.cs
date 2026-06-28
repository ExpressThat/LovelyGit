using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

public sealed class CommitGraphManager : IDisposable
{
    private readonly LovelyGitRepository _repository;
    private readonly Guid _repositoryId;
    private readonly string? _remoteUrl;
    private CommitGraphTraversalSession? _session;
    private bool _disposed;

    private CommitGraphManager(
        LovelyGitRepository repository,
        Guid repositoryId,
        string? remoteUrl)
    {
        _repository = repository;
        _repositoryId = repositoryId;
        _remoteUrl = remoteUrl;
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
            var remoteUrl = await GitRemoteConfigReader
                .ReadPrimaryRemoteUrlAsync(repository.GitDirectory, cancellationToken)
                .ConfigureAwait(false);
            return new CommitGraphOpenResult(
                true,
                new CommitGraphManager(repository, repositoryId, remoteUrl),
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
        _repository.ClearObjectCaches();

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
            var parents = GetGraphParents(commit);

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

            if (collectRows)
            {
                rows.Add(CommitGraphRowBuilder.Build(
                    commit,
                    parents,
                    rowIndex,
                    activeLaneTargets,
                    ref maxLaneCount,
                    _remoteUrl));
            }
            else
            {
                _ = CommitGraphRowBuilder.Build(
                    commit,
                    parents,
                    rowIndex,
                    activeLaneTargets,
                    ref maxLaneCount,
                    _remoteUrl);
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
            RemotePrefixes = _repository.RemotePrefixes.ToList(),
            CurrentBranchName = _repository.CurrentBranchName,
            HasMore = hasMore,
        };

        return response;
    }

    private async Task<CommitGraphTraversalSession> OpenTraversalSessionAsync(
        CommitGraphCursorState cursor,
        CancellationToken cancellationToken)
    {
        if (cursor.RepositoryId == _repositoryId
            && _session != null
            && _session.Offset == cursor.Offset)
        {
            return _session;
        }

        _session = await CreateTraversalSessionAsync(cancellationToken).ConfigureAwait(false);
        if (cursor.RepositoryId == _repositoryId && cursor.Offset > 0)
        {
            await ReadRowsAsync(_session, cursor.Offset, collectRows: false, cancellationToken)
                .ConfigureAwait(false);
        }

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
                created.EnqueueFrontier(hash, CommitGraphCommitPriority.FromCommit(head, IsStashRef(head)));
            }
        }

        created.SaveState(0, new List<string?>(), 0);
        return created;
    }

    private async Task<IReadOnlyList<GitCommit>> GetStartingCommitsAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetStartingCommitsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static List<string> GetGraphParents(GitCommit commit)
    {
        var parents = commit.ParentHashes.Select(parent => parent.ToString()).ToList();
        if (!IsStashRef(commit) || parents.Count <= 1)
        {
            return parents;
        }

        return [parents[0]];
    }

    private static bool IsStashRef(GitCommit commit)
    {
        return commit.Refs.Any(reference => reference.Kind == GitRefKind.Stash);
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
