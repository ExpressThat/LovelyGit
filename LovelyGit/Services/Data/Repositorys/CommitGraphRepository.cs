using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys
{
    public class CommitGraphRepository
    {
        private readonly GitRepoCacheDbContext _gitRepoCache;

        public CommitGraphRepository(GitRepoCacheDbContext gitRepoCache)
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
            await foreach (var entry in _gitRepoCache.CommitGraphFrontier.FindAllAsync()
                .ConfigureAwait(false))
            {
                if (entry.RepositoryId == repositoryId)
                {
                    yield return entry;
                }
            }
        }

        public async IAsyncEnumerable<CommitGraphSeenEntry> GetSeenAsync(Guid repositoryId)
        {
            await foreach (var entry in _gitRepoCache.CommitGraphSeen.FindAllAsync()
                .ConfigureAwait(false))
            {
                if (entry.RepositoryId == repositoryId)
                {
                    yield return entry;
                }
            }
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

        public async IAsyncEnumerable<CommitGraphCachedCommitEntry> GetCachedCommitEntriesAsync(Guid repositoryId)
        {
            await foreach (var entry in _gitRepoCache.CommitGraphCachedCommits.FindAllAsync()
                .ConfigureAwait(false))
            {
                if (entry.RepositoryId == repositoryId)
                {
                    yield return entry;
                }
            }
        }

        public async Task<bool> HasSeenAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            return await _gitRepoCache.CommitGraphSeen
                .FindByIdAsync(MakeRepositoryHashId(repositoryId, hash), cancellationToken)
                .ConfigureAwait(false) != null;
        }

        public async Task<bool> HasCommitDetailsAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            return await _gitRepoCache.CommitDetailsCache
                .FindByIdAsync(MakeRepositoryHashId(repositoryId, hash), cancellationToken)
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
            foreach (var row in response.Rows)
            {
                var id = MakeRepositoryRowId(repositoryId, row.RowIndex);
                var entry = new CommitGraphCachedCommitEntry
                {
                    Id = id,
                    RepositoryId = repositoryId,
                    RowIndex = row.RowIndex,
                    Hash = row.Commit.Hash,
                };

                if (await _gitRepoCache.CommitGraphCachedCommits.FindByIdAsync(id, cancellationToken).ConfigureAwait(false) == null)
                {
                    await _gitRepoCache.CommitGraphCachedCommits.InsertAsync(entry, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _gitRepoCache.CommitGraphCachedCommits.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
                }
            }

            await _gitRepoCache.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<CommitDetailsResponse?> GetCommitDetailsAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            var entry = await _gitRepoCache.CommitDetailsCache
                .FindByIdAsync(MakeRepositoryHashId(repositoryId, hash), cancellationToken)
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
            var id = MakeRepositoryHashId(repositoryId, hash);
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
                    Id = MakeRepositoryCommitFileId(repositoryId, hash, index),
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

        public async Task AddSeenAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            await _gitRepoCache.CommitGraphSeen.InsertAsync(
                new CommitGraphSeenEntry
                {
                    Id = MakeRepositoryHashId(repositoryId, hash),
                    RepositoryId = repositoryId,
                    Hash = hash,
                },
                cancellationToken).ConfigureAwait(false);
        }

        public async Task AddFrontierAsync(
            Guid repositoryId,
            string hash,
            long seconds,
            CancellationToken cancellationToken)
        {
            await _gitRepoCache.CommitGraphFrontier.InsertAsync(
                new CommitGraphFrontierEntry
                {
                    Id = MakeRepositoryHashId(repositoryId, hash),
                    RepositoryId = repositoryId,
                    Hash = hash,
                    Seconds = seconds,
                },
                cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteFrontierAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            await _gitRepoCache.CommitGraphFrontier
                .DeleteAsync(MakeRepositoryHashId(repositoryId, hash), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SaveRepositoryStateAsync(
            Guid repositoryId,
            int offset,
            int maxLaneCount,
            string lanes,
            CancellationToken cancellationToken)
        {
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
                await _gitRepoCache.CommitGraphStates.InsertAsync(state, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _gitRepoCache.CommitGraphStates.UpdateAsync(state, cancellationToken).ConfigureAwait(false);
            }

            await _gitRepoCache.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteTraversalEntriesAsync(Guid repositoryId)
        {
            await foreach (var entry in GetFrontierAsync(repositoryId).ConfigureAwait(false))
            {
                await _gitRepoCache.CommitGraphFrontier.DeleteAsync(entry.Id).ConfigureAwait(false);
            }

            await foreach (var entry in GetSeenAsync(repositoryId).ConfigureAwait(false))
            {
                await _gitRepoCache.CommitGraphSeen.DeleteAsync(entry.Id).ConfigureAwait(false);
            }

            await _gitRepoCache.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task ClearRepositoryAsync(Guid repositoryId)
        {
            await _gitRepoCache.CommitGraphStates.DeleteAsync(repositoryId).ConfigureAwait(false);

            await foreach (var entry in GetFrontierAsync(repositoryId).ConfigureAwait(false))
            {
                await _gitRepoCache.CommitGraphFrontier.DeleteAsync(entry.Id).ConfigureAwait(false);
            }

            await foreach (var entry in GetSeenAsync(repositoryId).ConfigureAwait(false))
            {
                await _gitRepoCache.CommitGraphSeen.DeleteAsync(entry.Id).ConfigureAwait(false);
            }

            await foreach (var entry in GetCachedCommitEntriesAsync(repositoryId).ConfigureAwait(false))
            {
                await _gitRepoCache.CommitGraphCachedCommits.DeleteAsync(entry.Id).ConfigureAwait(false);
            }

            await foreach (var entry in GetCommitDetailsCacheEntriesAsync(repositoryId).ConfigureAwait(false))
            {
                await _gitRepoCache.CommitDetailsCache.DeleteAsync(entry.Id).ConfigureAwait(false);
            }

            await foreach (var entry in GetCommitDetailsChangedFileEntriesAsync(repositoryId).ConfigureAwait(false))
            {
                await _gitRepoCache.CommitDetailsChangedFiles.DeleteAsync(entry.Id).ConfigureAwait(false);
            }

            await _gitRepoCache.SaveChangesAsync().ConfigureAwait(false);
        }

        private static string MakeRepositoryHashId(Guid repositoryId, string hash)
        {
            return string.Concat(repositoryId, ":", hash);
        }

        private static string MakeRepositoryRowId(Guid repositoryId, int rowIndex)
        {
            return string.Concat(repositoryId, ":", rowIndex);
        }

        private static string MakeRepositoryCommitFileId(Guid repositoryId, string hash, int fileIndex)
        {
            return string.Concat(repositoryId, ":", hash, ":", fileIndex);
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
}
