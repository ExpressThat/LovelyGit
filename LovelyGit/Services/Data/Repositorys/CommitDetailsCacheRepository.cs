using BLite.Core.Query;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed class CommitDetailsCacheRepository
{
    private readonly GitRepoCacheDbContext _gitRepoCache;
    private readonly object _saveGateLock = new();
    private readonly Dictionary<string, SaveGate> _saveGates = new(StringComparer.Ordinal);

    public CommitDetailsCacheRepository(GitRepoCacheDbContext gitRepoCache)
    {
        _gitRepoCache = gitRepoCache;
    }

    public async IAsyncEnumerable<CommitDetailsCacheEntry> GetCommitDetailsCacheEntriesAsync(Guid repositoryId)
    {
        var entries = await _gitRepoCache.CommitDetailsCache
            .AsQueryable()
            .Where(entry => entry.RepositoryId == repositoryId)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    public async IAsyncEnumerable<CommitChangedFileCacheEntry> GetCommitDetailsChangedFileEntriesAsync(Guid repositoryId)
    {
        var entries = await _gitRepoCache.CommitDetailsChangedFiles
            .AsQueryable()
            .Where(entry => entry.RepositoryId == repositoryId)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    public async Task<bool> HasCommitDetailsAsync(
        Guid repositoryId,
        string hash,
        CancellationToken cancellationToken)
    {
        return await _gitRepoCache.CommitDetailsCache
            .FindByIdAsync(CommitGraphCacheKeys.MakeRepositoryHashId(repositoryId, hash), cancellationToken)
            .ConfigureAwait(false) != null;
    }

    public async Task<CommitDetailsResponse?> GetCommitDetailsAsync(
        Guid repositoryId,
        string hash,
        CancellationToken cancellationToken)
    {
        var entry = await _gitRepoCache.CommitDetailsCache
            .FindByIdAsync(CommitGraphCacheKeys.MakeRepositoryHashId(repositoryId, hash), cancellationToken)
            .ConfigureAwait(false);

        if (entry == null)
        {
            return null;
        }

        var changedFiles = await GetCommitDetailsChangedFileEntriesAsync(repositoryId, hash, cancellationToken)
            .ConfigureAwait(false);

        return ToResponse(entry.Details, changedFiles);
    }

    public async Task SaveCommitDetailsAsync(
        Guid repositoryId,
        string hash,
        CommitDetailsResponse response,
        CancellationToken cancellationToken)
    {
        var id = CommitGraphCacheKeys.MakeRepositoryHashId(repositoryId, hash);
        var gate = GetSaveGate(id);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            var entry = new CommitDetailsCacheEntry
            {
                Id = id,
                RepositoryId = repositoryId,
                Hash = hash,
                Details = ToCache(response),
            };

            var existingFileEntries = await GetCommitDetailsChangedFileEntriesAsync(repositoryId, hash, cancellationToken)
                .ConfigureAwait(false);

            using var transaction = _gitRepoCache.BeginTransaction();

            foreach (var fileEntry in existingFileEntries)
            {
                await _gitRepoCache.CommitDetailsChangedFiles
                    .DeleteAsync(fileEntry.Id, transaction, cancellationToken)
                    .ConfigureAwait(false);
            }

            for (var index = 0; index < response.ChangedFiles.Count; index++)
            {
                var file = response.ChangedFiles[index];
                var fileEntry = new CommitChangedFileCacheEntry
                {
                    Id = CommitGraphCacheKeys.MakeRepositoryCommitFileId(repositoryId, hash, index),
                    RepositoryId = repositoryId,
                    Hash = hash,
                    FileIndex = index,
                    File = new CommitChangedFileCache
                    {
                        Path = file.Path,
                        Status = file.Status,
                        Additions = file.Additions,
                        Deletions = file.Deletions,
                        IsBinary = file.IsBinary,
                    },
                };

                await _gitRepoCache.CommitDetailsChangedFiles
                    .InsertAsync(fileEntry, transaction, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (await _gitRepoCache.CommitDetailsCache.FindByIdAsync(id, cancellationToken).ConfigureAwait(false) == null)
            {
                await _gitRepoCache.CommitDetailsCache.InsertAsync(entry, transaction, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _gitRepoCache.CommitDetailsCache.UpdateAsync(entry, transaction, cancellationToken).ConfigureAwait(false);
            }

            await _gitRepoCache.SaveChangesAsync(transaction, cancellationToken).ConfigureAwait(false);
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

    private async Task<List<CommitChangedFileCacheEntry>> GetCommitDetailsChangedFileEntriesAsync(
        Guid repositoryId,
        string hash,
        CancellationToken cancellationToken)
    {
        return await _gitRepoCache.CommitDetailsChangedFiles
            .AsQueryable()
            .Where(entry => entry.RepositoryId == repositoryId && entry.Hash == hash)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static CommitDetailsCache ToCache(CommitDetailsResponse response)
    {
        return new CommitDetailsCache
        {
            Hash = response.Hash ?? string.Empty,
            Parents = response.Parents?.ToList() ?? [],
            Author = response.Author ?? string.Empty,
            Email = response.Email ?? string.Empty,
            Date = response.Date,
            Subject = response.Subject ?? string.Empty,
            Body = response.Body ?? string.Empty,
            Message = response.Message ?? string.Empty,
            Branches = response.Branches?.ToList() ?? [],
            Tags = response.Tags?.ToList() ?? [],
            Stats = new CommitStatsCache
            {
                Additions = response.Stats?.Additions ?? 0,
                Deletions = response.Stats?.Deletions ?? 0,
            },
        };
    }

    private static CommitDetailsResponse ToResponse(
        CommitDetailsCache cache,
        IEnumerable<CommitChangedFileCacheEntry> changedFiles)
    {
        return new CommitDetailsResponse
        {
            Hash = cache.Hash ?? string.Empty,
            Parents = cache.Parents?.ToList() ?? [],
            Author = cache.Author ?? string.Empty,
            Email = cache.Email ?? string.Empty,
            Date = cache.Date,
            Subject = cache.Subject ?? string.Empty,
            Body = cache.Body ?? string.Empty,
            Message = cache.Message ?? string.Empty,
            Branches = cache.Branches?.ToList() ?? [],
            Tags = cache.Tags?.ToList() ?? [],
            Stats = new CommitStats
            {
                Additions = ToUInt32(cache.Stats?.Additions ?? 0),
                Deletions = ToUInt32(cache.Stats?.Deletions ?? 0),
            },
            ChangedFiles = changedFiles
                .OrderBy(entry => entry.FileIndex)
                .Select(entry => entry.File)
                .Where(file => file != null)
                .Select(file => new CommitChangedFile
                {
                    Path = file.Path ?? string.Empty,
                    Status = file.Status ?? string.Empty,
                    Additions = ToUInt32(file.Additions),
                    Deletions = ToUInt32(file.Deletions),
                    IsBinary = file.IsBinary,
                })
                .ToList(),
        };
    }

    private static uint ToUInt32(long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value > uint.MaxValue ? uint.MaxValue : (uint)value;
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
