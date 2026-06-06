using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed class CommitDetailsCacheRepository
{
    private readonly GitRepoCacheDbContext _gitRepoCache;

    public CommitDetailsCacheRepository(GitRepoCacheDbContext gitRepoCache)
    {
        _gitRepoCache = gitRepoCache;
    }

    public async IAsyncEnumerable<CommitDetailsCacheEntry> GetCommitDetailsCacheEntriesAsync(Guid repositoryId)
    {
        await foreach (var entry in _gitRepoCache.CommitDetailsCache.FindAllAsync()
            .ConfigureAwait(false))
        {
            if (entry.RepositoryId == repositoryId)
            {
                yield return entry;
            }
        }
    }

    public async IAsyncEnumerable<CommitChangedFileCacheEntry> GetCommitDetailsChangedFileEntriesAsync(Guid repositoryId)
    {
        await foreach (var entry in _gitRepoCache.CommitDetailsChangedFiles.FindAllAsync()
            .ConfigureAwait(false))
        {
            if (entry.RepositoryId == repositoryId)
            {
                yield return entry;
            }
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

        var changedFiles = new List<CommitChangedFileCacheEntry>();
        await foreach (var fileEntry in GetCommitDetailsChangedFileEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(fileEntry.Hash, hash, StringComparison.Ordinal))
            {
                changedFiles.Add(fileEntry);
            }
        }

        return ToResponse(entry.Details, changedFiles);
    }

    public async Task SaveCommitDetailsAsync(
        Guid repositoryId,
        string hash,
        CommitDetailsResponse response,
        CancellationToken cancellationToken)
    {
        var id = CommitGraphCacheKeys.MakeRepositoryHashId(repositoryId, hash);
        var entry = new CommitDetailsCacheEntry
        {
            Id = id,
            RepositoryId = repositoryId,
            Hash = hash,
            Details = ToCache(response),
        };

        var existingFileEntries = new List<CommitChangedFileCacheEntry>();
        await foreach (var fileEntry in GetCommitDetailsChangedFileEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(fileEntry.Hash, hash, StringComparison.Ordinal))
            {
                existingFileEntries.Add(fileEntry);
            }
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

            if (await _gitRepoCache.CommitDetailsChangedFiles.FindByIdAsync(fileEntry.Id, cancellationToken).ConfigureAwait(false) == null)
            {
                await _gitRepoCache.CommitDetailsChangedFiles
                    .InsertAsync(fileEntry, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await _gitRepoCache.CommitDetailsChangedFiles
                    .UpdateAsync(fileEntry, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        foreach (var fileEntry in existingFileEntries)
        {
            if (fileEntry.FileIndex >= response.ChangedFiles.Count)
            {
                await _gitRepoCache.CommitDetailsChangedFiles
                    .DeleteAsync(fileEntry.Id, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (await _gitRepoCache.CommitDetailsCache.FindByIdAsync(id, cancellationToken).ConfigureAwait(false) == null)
        {
            await _gitRepoCache.CommitDetailsCache.InsertAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _gitRepoCache.CommitDetailsCache.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        }

        await _gitRepoCache.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static CommitDetailsCache ToCache(CommitDetailsResponse response)
    {
        return new CommitDetailsCache
        {
            Hash = response.Hash,
            Parents = response.Parents.ToList(),
            Author = response.Author,
            Email = response.Email,
            Date = response.Date,
            Subject = response.Subject,
            Body = response.Body,
            Message = response.Message,
            Branches = response.Branches.ToList(),
            Tags = response.Tags.ToList(),
            Stats = new CommitStatsCache
            {
                Additions = response.Stats.Additions,
                Deletions = response.Stats.Deletions,
            },
        };
    }

    private static CommitDetailsResponse ToResponse(
        CommitDetailsCache cache,
        IEnumerable<CommitChangedFileCacheEntry> changedFiles)
    {
        return new CommitDetailsResponse
        {
            Hash = cache.Hash,
            Parents = cache.Parents.ToList(),
            Author = cache.Author,
            Email = cache.Email,
            Date = cache.Date,
            Subject = cache.Subject,
            Body = cache.Body,
            Message = cache.Message,
            Branches = cache.Branches.ToList(),
            Tags = cache.Tags.ToList(),
            Stats = new CommitStats
            {
                Additions = ToUInt32(cache.Stats.Additions),
                Deletions = ToUInt32(cache.Stats.Deletions),
            },
            ChangedFiles = changedFiles
                .OrderBy(entry => entry.FileIndex)
                .Select(entry => entry.File)
                .Select(file => new CommitChangedFile
                {
                    Path = file.Path,
                    Status = file.Status,
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
}
