using BLite.Core.Query;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using System.Security.Cryptography;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed partial class CommitFileDiffCacheRepository
{
    private async Task InsertLineEntriesBatchAsync(
        IReadOnlyList<CommitFileDiffLineCacheEntry> lineEntries,
        CancellationToken cancellationToken)
    {
        using var transaction = _gitRepoCache.BeginTransaction();
        foreach (var lineEntry in lineEntries)
        {
            await _gitRepoCache.CommitFileDiffLines
                .InsertAsync(lineEntry, transaction, cancellationToken)
                .ConfigureAwait(false);
        }

        await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearCommitFileDiffsAsync(
        Guid repositoryId,
        string hash,
        CancellationToken cancellationToken)
    {
        var entries = await _gitRepoCache.CommitFileDiffs
            .AsQueryable()
            .Where(entry => entry.RepositoryId == repositoryId && entry.Hash == hash)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        using (var transaction = _gitRepoCache.BeginTransaction())
        {
            foreach (var entry in entries)
            {
                await _gitRepoCache.CommitFileDiffs.DeleteAsync(entry.Id, transaction, cancellationToken).ConfigureAwait(false);
            }

            await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
        }

        foreach (var entry in entries)
        {
            await DeleteLineEntriesInBatchesAsync(entry, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RemoveCommitFileDiffAsync(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var id = MakeDiffId(repositoryId, hash, path, viewMode, ignoreWhitespace);
        var gate = GetSaveGate(id);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;
            var entry = await _gitRepoCache.CommitFileDiffs
                .FindByIdAsync(id, cancellationToken)
                .ConfigureAwait(false);
            if (entry == null) return;

            using (var transaction = _gitRepoCache.BeginTransaction())
            {
                await _gitRepoCache.CommitFileDiffs
                    .DeleteAsync(id, transaction, cancellationToken)
                    .ConfigureAwait(false);
                await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
            }

            await DeleteLineEntriesInBatchesAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (enteredGate) gate.Semaphore.Release();
            ReleaseSaveGate(id, gate);
        }
    }

    private async Task DeleteLineEntriesInBatchesAsync(
        CommitFileDiffCacheEntry entry,
        CancellationToken cancellationToken)
    {
        await DeleteLineEntriesInBatchesAsync(entry.Id, entry.LineCount, cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteLineEntriesInBatchesAsync(
        string diffId,
        int lineCount,
        CancellationToken cancellationToken)
    {
        var lineLookupId = MakeDiffLineLookupId(diffId);
        for (var offset = 0; offset < lineCount; offset += LineTransactionBatchSize)
        {
            using var transaction = _gitRepoCache.BeginTransaction();
            var end = Math.Min(offset + LineTransactionBatchSize, lineCount);
            for (var index = offset; index < end; index++)
            {
                await _gitRepoCache.CommitFileDiffLines
                    .DeleteAsync(MakeLineId(lineLookupId, index), transaction, cancellationToken)
                    .ConfigureAwait(false);
            }

            await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DeleteLineEntriesInBatchesAsync(
        IReadOnlyList<CommitFileDiffLineCacheEntry> lineEntries,
        CancellationToken cancellationToken)
    {
        for (var offset = 0; offset < lineEntries.Count; offset += LineTransactionBatchSize)
        {
            using var transaction = _gitRepoCache.BeginTransaction();
            var end = Math.Min(offset + LineTransactionBatchSize, lineEntries.Count);
            for (var index = offset; index < end; index++)
            {
                await _gitRepoCache.CommitFileDiffLines
                    .DeleteAsync(lineEntries[index].Id, transaction, cancellationToken)
                    .ConfigureAwait(false);
            }

            await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<List<CommitFileDiffLineCacheEntry>> GetLineEntriesAsync(
        CommitFileDiffCacheEntry entry,
        CancellationToken cancellationToken)
    {
        var lineEntries = new List<CommitFileDiffLineCacheEntry>(entry.LineCount);
        var lineLookupId = MakeDiffLineLookupId(entry.Id);
        for (var index = 0; index < entry.LineCount; index++)
        {
            var lineEntry = await _gitRepoCache.CommitFileDiffLines
                .FindByIdAsync(MakeLineId(lineLookupId, index), cancellationToken)
                .ConfigureAwait(false);
            if (lineEntry != null)
            {
                lineEntries.Add(lineEntry);
            }
        }

        return lineEntries;
    }

    private async Task DeleteDiffEntryAsync(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        BLite.Core.Transactions.ITransaction transaction,
        CancellationToken cancellationToken)
    {
        var id = MakeDiffId(repositoryId, hash, path, viewMode, ignoreWhitespace);
        if (await _gitRepoCache.CommitFileDiffs.FindByIdAsync(id, cancellationToken).ConfigureAwait(false) != null)
        {
            await _gitRepoCache.CommitFileDiffs.DeleteAsync(id, transaction, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string MakeDiffId(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        var whitespaceMode = ignoreWhitespace ? "ignore-ws" : "exact";
        return $"{repositoryId:N}:{hash}:{viewMode}:{whitespaceMode}:{Uri.EscapeDataString(path)}";
    }

    private static string MakeLineId(
        string diffLineLookupId,
        int index)
    {
        return string.Create(
            diffLineLookupId.Length + 9,
            (diffLineLookupId, index),
            static (destination, state) =>
            {
                state.diffLineLookupId.AsSpan().CopyTo(destination);
                destination[state.diffLineLookupId.Length] = ':';
                state.index.TryFormat(destination[(state.diffLineLookupId.Length + 1)..], out _, "D8");
            });
    }

    private static string MakeDiffLineLookupId(string diffId)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(diffId)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

}
