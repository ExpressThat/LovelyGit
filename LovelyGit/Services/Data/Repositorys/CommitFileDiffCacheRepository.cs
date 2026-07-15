using BLite.Core.Query;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using System.Security.Cryptography;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed partial class CommitFileDiffCacheRepository
{
    private const int LineTransactionBatchSize = 250;
    private const int MaxCachedLineLength = 12000;
    private readonly GitRepoCacheDbContext _gitRepoCache;
    private readonly object _saveGateLock = new();
    private readonly Dictionary<string, SaveGate> _saveGates = new(StringComparer.Ordinal);

    public CommitFileDiffCacheRepository(GitRepoCacheDbContext gitRepoCache)
    {
        _gitRepoCache = gitRepoCache;
    }

    public async IAsyncEnumerable<CommitFileDiffCacheEntry> GetCommitFileDiffEntriesAsync(Guid repositoryId)
    {
        var entries = await _gitRepoCache.CommitFileDiffs
            .AsQueryable()
            .Where(entry => entry.RepositoryId == repositoryId)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    public async IAsyncEnumerable<CommitFileDiffLineCacheEntry> GetCommitFileDiffLineEntriesAsync(Guid repositoryId)
    {
        var diffEntries = await _gitRepoCache.CommitFileDiffs
            .AsQueryable()
            .Where(entry => entry.RepositoryId == repositoryId)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var diffEntry in diffEntries)
        {
            var lineEntries = await GetLineEntriesAsync(diffEntry, CancellationToken.None).ConfigureAwait(false);
            foreach (var lineEntry in lineEntries)
            {
                yield return lineEntry;
            }
        }
    }

    public async Task<CommitFileDiffResponse?> GetCommitFileDiffAsync(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var id = MakeDiffId(repositoryId, hash, path, viewMode, ignoreWhitespace);
        var entry = await _gitRepoCache.CommitFileDiffs
            .FindByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (entry == null)
        {
            return null;
        }

        var lines = await GetLineEntriesAsync(entry, cancellationToken).ConfigureAwait(false);

        return ToResponse(entry, lines);
    }

    public async Task<bool> HasCommitFileDiffAsync(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        return await _gitRepoCache.CommitFileDiffs
            .FindByIdAsync(MakeDiffId(repositoryId, hash, path, viewMode, ignoreWhitespace), cancellationToken)
            .ConfigureAwait(false) != null;
    }

    public async Task SaveCommitFileDiffAsync(
        Guid repositoryId,
        string hash,
        string path,
        CommitFileDiffResponse response,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var id = MakeDiffId(repositoryId, hash, path, response.ViewMode, ignoreWhitespace);
        var gate = GetSaveGate(id);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            var entry = CreateCacheEntry(
                id, repositoryId, hash, path, response, ignoreWhitespace);

            var existingEntry = await _gitRepoCache.CommitFileDiffs
                .FindByIdAsync(id, cancellationToken)
                .ConfigureAwait(false);

            using (var metadataDeleteTransaction = _gitRepoCache.BeginTransaction())
            {
                using var transactionRetention = BLiteTransactionRetention.Track(metadataDeleteTransaction);
                await DeleteDiffEntryAsync(
                        repositoryId,
                        hash,
                        path,
                        response.ViewMode,
                        ignoreWhitespace,
                        metadataDeleteTransaction,
                        cancellationToken)
                    .ConfigureAwait(false);
                await _gitRepoCache.SaveChangesAsync(metadataDeleteTransaction, cancellationToken).ConfigureAwait(false);
            }

            await DeleteLineEntriesInBatchesAsync(
                    id,
                    Math.Max(existingEntry?.LineCount ?? 0, response.Lines.Count),
                    cancellationToken)
                .ConfigureAwait(false);

            var pendingLineEntries = new List<CommitFileDiffLineCacheEntry>(LineTransactionBatchSize);
            var lineLookupId = MakeDiffLineLookupId(id);
            for (var index = 0; index < response.Lines.Count; index++)
            {
                pendingLineEntries.Add(new CommitFileDiffLineCacheEntry
                {
                    Id = MakeLineId(lineLookupId, index),
                    RepositoryId = repositoryId,
                    Hash = hash,
                    Path = path,
                    ViewMode = response.ViewMode.ToString(),
                    IgnoreWhitespace = ignoreWhitespace,
                    DiffId = lineLookupId,
                    LineIndex = index,
                    Line = ToCache(Normalize(response.Lines[index])),
                });

                if (pendingLineEntries.Count >= LineTransactionBatchSize)
                {
                    await InsertLineEntriesBatchAsync(pendingLineEntries, cancellationToken).ConfigureAwait(false);
                    pendingLineEntries.Clear();
                }
            }

            if (pendingLineEntries.Count > 0)
            {
                await InsertLineEntriesBatchAsync(pendingLineEntries, cancellationToken).ConfigureAwait(false);
            }

            using var metadataInsertTransaction = _gitRepoCache.BeginTransaction();
            using var metadataInsertRetention = BLiteTransactionRetention.Track(metadataInsertTransaction);
            await _gitRepoCache.CommitFileDiffs
                .InsertAsync(entry, metadataInsertTransaction, cancellationToken)
                .ConfigureAwait(false);
            await _gitRepoCache.SaveChangesAsync(metadataInsertTransaction, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (enteredGate)
            {
                gate.Semaphore.Release();
            }

            ReleaseSaveGate(id, gate);
        }
    }

}
