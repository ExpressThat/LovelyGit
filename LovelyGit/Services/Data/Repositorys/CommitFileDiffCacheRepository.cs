using BLite.Core.Query;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using System.Security.Cryptography;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed class CommitFileDiffCacheRepository
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
        CancellationToken cancellationToken)
    {
        var id = MakeDiffId(repositoryId, hash, path, viewMode);
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
        CancellationToken cancellationToken)
    {
        return await _gitRepoCache.CommitFileDiffs
            .FindByIdAsync(MakeDiffId(repositoryId, hash, path, viewMode), cancellationToken)
            .ConfigureAwait(false) != null;
    }

    public async Task SaveCommitFileDiffAsync(
        Guid repositoryId,
        string hash,
        string path,
        CommitFileDiffResponse response,
        CancellationToken cancellationToken)
    {
        var id = MakeDiffId(repositoryId, hash, path, response.ViewMode);
        var gate = GetSaveGate(id);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            var entry = new CommitFileDiffCacheEntry
            {
                Id = id,
                RepositoryId = repositoryId,
                Hash = hash,
                Path = path,
                ViewMode = response.ViewMode.ToString(),
                Status = response.Status,
                IsBinary = response.IsBinary,
                HasDifferences = response.HasDifferences,
                LineCount = response.Lines.Count,
            };

            var existingEntry = await _gitRepoCache.CommitFileDiffs
                .FindByIdAsync(id, cancellationToken)
                .ConfigureAwait(false);

            using (var metadataDeleteTransaction = _gitRepoCache.BeginTransaction())
            {
                await DeleteDiffEntryAsync(repositoryId, hash, path, response.ViewMode, metadataDeleteTransaction, cancellationToken)
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
        BLite.Core.Transactions.ITransaction transaction,
        CancellationToken cancellationToken)
    {
        var id = MakeDiffId(repositoryId, hash, path, viewMode);
        if (await _gitRepoCache.CommitFileDiffs.FindByIdAsync(id, cancellationToken).ConfigureAwait(false) != null)
        {
            await _gitRepoCache.CommitFileDiffs.DeleteAsync(id, transaction, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string MakeDiffId(
        Guid repositoryId,
        string hash,
        string path,
        CommitDiffViewMode viewMode)
    {
        return $"{repositoryId:N}:{hash}:{viewMode}:{Uri.EscapeDataString(path)}";
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

    private static CommitFileDiffLine Normalize(CommitFileDiffLine line)
    {
        var oldText = Truncate(line.OldText);
        var newText = Truncate(line.NewText);
        var text = Truncate(line.Text);
        return new CommitFileDiffLine
        {
            OldLineNumber = line.OldLineNumber,
            NewLineNumber = line.NewLineNumber,
            OldText = oldText,
            NewText = newText,
            Text = text,
            ChangeType = line.ChangeType,
            OldSyntaxSpans = TrimSpans(line.OldSyntaxSpans, oldText.Length),
            NewSyntaxSpans = TrimSpans(line.NewSyntaxSpans, newText.Length),
            SyntaxSpans = TrimSpans(line.SyntaxSpans, text.Length),
            OldChangeSpans = TrimSpans(line.OldChangeSpans, oldText.Length),
            NewChangeSpans = TrimSpans(line.NewChangeSpans, newText.Length),
            ChangeSpans = TrimSpans(line.ChangeSpans, text.Length),
        };
    }

    private static string Truncate(string value)
    {
        if (value.Length <= MaxCachedLineLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, MaxCachedLineLength), " ... [line truncated]");
    }

    private static List<CommitFileDiffSyntaxSpan> TrimSpans(
        IEnumerable<CommitFileDiffSyntaxSpan> spans,
        int maxLength)
    {
        var trimmed = new List<CommitFileDiffSyntaxSpan>();
        foreach (var span in spans)
        {
            if (span.Start >= maxLength)
            {
                continue;
            }

            var length = Math.Min(span.Length, maxLength - span.Start);
            if (length <= 0)
            {
                continue;
            }

            trimmed.Add(new CommitFileDiffSyntaxSpan
            {
                Start = span.Start,
                Length = length,
                Scope = span.Scope,
            });
        }

        return trimmed;
    }

    private static List<CommitFileDiffChangeSpan> TrimSpans(
        IEnumerable<CommitFileDiffChangeSpan> spans,
        int maxLength)
    {
        var trimmed = new List<CommitFileDiffChangeSpan>();
        foreach (var span in spans)
        {
            if (span.Start >= maxLength)
            {
                continue;
            }

            var length = Math.Min(span.Length, maxLength - span.Start);
            if (length <= 0)
            {
                continue;
            }

            trimmed.Add(new CommitFileDiffChangeSpan
            {
                Start = span.Start,
                Length = length,
                ChangeType = span.ChangeType,
            });
        }

        return trimmed;
    }

    private static CommitFileDiffLineCache ToCache(CommitFileDiffLine line)
    {
        return new CommitFileDiffLineCache
        {
            OldLineNumber = line.OldLineNumber,
            NewLineNumber = line.NewLineNumber,
            OldText = line.OldText,
            NewText = line.NewText,
            Text = line.Text,
            ChangeType = line.ChangeType,
            OldSyntaxSpans = ToCache(line.OldSyntaxSpans),
            NewSyntaxSpans = ToCache(line.NewSyntaxSpans),
            SyntaxSpans = ToCache(line.SyntaxSpans),
            OldChangeSpans = ToCache(line.OldChangeSpans),
            NewChangeSpans = ToCache(line.NewChangeSpans),
            ChangeSpans = ToCache(line.ChangeSpans),
        };
    }

    private static CommitFileDiffResponse ToResponse(
        CommitFileDiffCacheEntry cache,
        List<CommitFileDiffLineCacheEntry> lines)
    {
        return new CommitFileDiffResponse
        {
            CommitHash = cache.Hash,
            Path = cache.Path,
            Status = cache.Status,
            ViewMode = Enum.TryParse<CommitDiffViewMode>(cache.ViewMode, out var viewMode)
                ? viewMode
                : CommitDiffViewMode.SideBySide,
            IsBinary = cache.IsBinary,
            HasDifferences = cache.HasDifferences,
            Lines = ToResponseLines(lines),
        };
    }

    private static List<CommitFileDiffLine> ToResponseLines(List<CommitFileDiffLineCacheEntry> lines)
    {
        lines.Sort(static (left, right) => left.LineIndex.CompareTo(right.LineIndex));
        var responseLines = new List<CommitFileDiffLine>(lines.Count);
        foreach (var line in lines)
        {
            responseLines.Add(ToResponse(line.Line));
        }

        return responseLines;
    }

    private static CommitFileDiffLine ToResponse(CommitFileDiffLineCache line)
    {
        return new CommitFileDiffLine
        {
            OldLineNumber = line.OldLineNumber,
            NewLineNumber = line.NewLineNumber,
            OldText = line.OldText,
            NewText = line.NewText,
            Text = line.Text,
            ChangeType = line.ChangeType,
            OldSyntaxSpans = ToResponse(line.OldSyntaxSpans),
            NewSyntaxSpans = ToResponse(line.NewSyntaxSpans),
            SyntaxSpans = ToResponse(line.SyntaxSpans),
            OldChangeSpans = ToResponse(line.OldChangeSpans),
            NewChangeSpans = ToResponse(line.NewChangeSpans),
            ChangeSpans = ToResponse(line.ChangeSpans),
        };
    }

    private static List<CommitFileDiffSyntaxSpanCache> ToCache(IEnumerable<CommitFileDiffSyntaxSpan> spans)
    {
        var cached = new List<CommitFileDiffSyntaxSpanCache>();
        foreach (var span in spans)
        {
            cached.Add(new CommitFileDiffSyntaxSpanCache
            {
                Start = span.Start,
                Length = span.Length,
                Scope = span.Scope,
            });
        }

        return cached;
    }

    private static List<CommitFileDiffSyntaxSpan> ToResponse(IEnumerable<CommitFileDiffSyntaxSpanCache> spans)
    {
        var response = new List<CommitFileDiffSyntaxSpan>();
        foreach (var span in spans)
        {
            response.Add(new CommitFileDiffSyntaxSpan
            {
                Start = span.Start,
                Length = span.Length,
                Scope = span.Scope,
            });
        }

        return response;
    }

    private static List<CommitFileDiffChangeSpanCache> ToCache(IEnumerable<CommitFileDiffChangeSpan> spans)
    {
        var cached = new List<CommitFileDiffChangeSpanCache>();
        foreach (var span in spans)
        {
            cached.Add(new CommitFileDiffChangeSpanCache
            {
                Start = span.Start,
                Length = span.Length,
                ChangeType = span.ChangeType,
            });
        }

        return cached;
    }

    private static List<CommitFileDiffChangeSpan> ToResponse(IEnumerable<CommitFileDiffChangeSpanCache> spans)
    {
        var response = new List<CommitFileDiffChangeSpan>();
        foreach (var span in spans)
        {
            response.Add(new CommitFileDiffChangeSpan
            {
                Start = span.Start,
                Length = span.Length,
                ChangeType = span.ChangeType,
            });
        }

        return response;
    }

    private SaveGate GetSaveGate(string key)
    {
        lock (_saveGateLock)
        {
            if (!_saveGates.TryGetValue(key, out var gate))
            {
                gate = new SaveGate();
                _saveGates[key] = gate;
            }

            gate.ReferenceCount++;
            return gate;
        }
    }

    private void ReleaseSaveGate(string key, SaveGate gate)
    {
        lock (_saveGateLock)
        {
            gate.ReferenceCount--;
            if (gate.ReferenceCount == 0
                && _saveGates.TryGetValue(key, out var activeGate)
                && ReferenceEquals(activeGate, gate))
            {
                _saveGates.Remove(key);
                gate.Semaphore.Dispose();
            }
        }
    }

    private sealed class SaveGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }
    }
}
