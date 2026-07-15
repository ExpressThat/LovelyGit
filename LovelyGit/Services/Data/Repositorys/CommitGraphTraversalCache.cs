using BLite.Core.Query;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed class CommitGraphTraversalCache
{
    private readonly GitRepoCacheDbContext _gitRepoCache;

    public CommitGraphTraversalCache(GitRepoCacheDbContext gitRepoCache)
    {
        _gitRepoCache = gitRepoCache;
    }

    public Task<CommitGraphRepositoryState?> GetRepositoryStateAsync(
        Guid repositoryId,
        CancellationToken cancellationToken)
    {
        return _gitRepoCache.CommitGraphStates.FindByIdAsync(repositoryId, cancellationToken).AsTask();
    }

    public async IAsyncEnumerable<CommitGraphFrontierEntry> GetFrontierAsync(Guid repositoryId)
    {
        var entries = await _gitRepoCache.CommitGraphFrontier
            .AsQueryable()
            .Where(entry => entry.RepositoryId == repositoryId)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    public async IAsyncEnumerable<CommitGraphSeenEntry> GetSeenAsync(Guid repositoryId)
    {
        var entries = await _gitRepoCache.CommitGraphSeen
            .AsQueryable()
            .Where(entry => entry.RepositoryId == repositoryId)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    public async IAsyncEnumerable<CommitGraphCachedCommitEntry> GetCachedCommitEntriesAsync(Guid repositoryId)
    {
        var entries = await _gitRepoCache.CommitGraphCachedCommits
            .AsQueryable()
            .Where(entry => entry.RepositoryId == repositoryId)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    public async Task<bool> HasSeenAsync(
        Guid repositoryId,
        string hash,
        CancellationToken cancellationToken)
    {
        return await _gitRepoCache.CommitGraphSeen
            .FindByIdAsync(CommitGraphCacheKeys.MakeRepositoryHashId(repositoryId, hash), cancellationToken)
            .ConfigureAwait(false) != null;
    }

    public async Task<List<CommitGraphCachedCommitEntry>> GetCachedCommitsAsync(
        Guid repositoryId,
        CancellationToken cancellationToken)
    {
        var entries = new List<CommitGraphCachedCommitEntry>();
        await foreach (var entry in GetCachedCommitEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add(entry);
        }

        return entries
            .OrderBy(entry => entry.RowIndex)
            .ToList();
    }

    public async Task SaveCachedCommitsAsync(
        Guid repositoryId,
        CommitGraphResponse response,
        CancellationToken cancellationToken)
    {
        using var transaction = _gitRepoCache.BeginTransaction();
        using var transactionRetention = BLiteTransactionRetention.Track(transaction);
        foreach (var row in response.Rows)
        {
            var id = CommitGraphCacheKeys.MakeRepositoryRowId(repositoryId, row.RowIndex);
            var entry = new CommitGraphCachedCommitEntry
            {
                Id = id,
                RepositoryId = repositoryId,
                RowIndex = row.RowIndex,
                Hash = row.Commit.Hash,
            };

            if (await _gitRepoCache.CommitGraphCachedCommits.FindByIdAsync(id, cancellationToken).ConfigureAwait(false) == null)
            {
                await _gitRepoCache.CommitGraphCachedCommits.InsertAsync(entry, transaction, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _gitRepoCache.CommitGraphCachedCommits.UpdateAsync(entry, transaction, cancellationToken).ConfigureAwait(false);
            }
        }

        await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddSeenAsync(
        Guid repositoryId,
        string hash,
        CancellationToken cancellationToken)
    {
        using var transaction = _gitRepoCache.BeginTransaction();
        using var transactionRetention = BLiteTransactionRetention.Track(transaction);
        await _gitRepoCache.CommitGraphSeen.InsertAsync(
            new CommitGraphSeenEntry
            {
                Id = CommitGraphCacheKeys.MakeRepositoryHashId(repositoryId, hash),
                RepositoryId = repositoryId,
                Hash = hash,
            },
            transaction,
            cancellationToken).ConfigureAwait(false);
        await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddFrontierAsync(
        Guid repositoryId,
        string hash,
        long seconds,
        CancellationToken cancellationToken)
    {
        using var transaction = _gitRepoCache.BeginTransaction();
        using var transactionRetention = BLiteTransactionRetention.Track(transaction);
        await _gitRepoCache.CommitGraphFrontier.InsertAsync(
            new CommitGraphFrontierEntry
            {
                Id = CommitGraphCacheKeys.MakeRepositoryHashId(repositoryId, hash),
                RepositoryId = repositoryId,
                Hash = hash,
                Seconds = seconds,
            },
            transaction,
            cancellationToken).ConfigureAwait(false);
        await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteFrontierAsync(
        Guid repositoryId,
        string hash,
        CancellationToken cancellationToken)
    {
        using var transaction = _gitRepoCache.BeginTransaction();
        using var transactionRetention = BLiteTransactionRetention.Track(transaction);
        await _gitRepoCache.CommitGraphFrontier
            .DeleteAsync(
                CommitGraphCacheKeys.MakeRepositoryHashId(repositoryId, hash),
                transaction,
                cancellationToken)
            .ConfigureAwait(false);
        await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveRepositoryStateAsync(
        Guid repositoryId,
        int offset,
        int maxLaneCount,
        string lanes,
        CancellationToken cancellationToken)
    {
        using var transaction = _gitRepoCache.BeginTransaction();
        using var transactionRetention = BLiteTransactionRetention.Track(transaction);
        var state = new CommitGraphRepositoryState
        {
            Id = repositoryId,
            RepositoryId = repositoryId,
            Offset = offset,
            MaxLaneCount = maxLaneCount,
            Lanes = lanes,
        };

        if (await GetRepositoryStateAsync(repositoryId, cancellationToken).ConfigureAwait(false) == null)
        {
            await _gitRepoCache.CommitGraphStates.InsertAsync(state, transaction, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _gitRepoCache.CommitGraphStates.UpdateAsync(state, transaction, cancellationToken).ConfigureAwait(false);
        }

        await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteTraversalEntriesAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        using var transaction = _gitRepoCache.BeginTransaction();
        using var transactionRetention = BLiteTransactionRetention.Track(transaction);
        await _gitRepoCache.CommitGraphStates.DeleteAsync(repositoryId, transaction, cancellationToken).ConfigureAwait(false);

        await foreach (var entry in GetFrontierAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _gitRepoCache.CommitGraphFrontier.DeleteAsync(entry.Id, transaction, cancellationToken).ConfigureAwait(false);
        }

        await foreach (var entry in GetSeenAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _gitRepoCache.CommitGraphSeen.DeleteAsync(entry.Id, transaction, cancellationToken).ConfigureAwait(false);
        }

        await foreach (var entry in GetCachedCommitEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _gitRepoCache.CommitGraphCachedCommits.DeleteAsync(entry.Id, transaction, cancellationToken).ConfigureAwait(false);
        }

        await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
    }
}
