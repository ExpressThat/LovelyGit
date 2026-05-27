using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;

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
            string repositoryId,
            CancellationToken cancellationToken)
        {
            return _gitRepoCache.CommitGraphStates.FindByIdAsync(repositoryId, cancellationToken).AsTask();
        }

        public async IAsyncEnumerable<CommitGraphFrontierEntry> GetFrontierAsync(
            string repositoryId,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var entry in _gitRepoCache.CommitGraphFrontier.FindAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                if (entry.RepositoryId == repositoryId)
                {
                    yield return entry;
                }
            }
        }

        public async IAsyncEnumerable<CommitGraphSeenEntry> GetSeenAsync(
            string repositoryId,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var entry in _gitRepoCache.CommitGraphSeen.FindAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                if (entry.RepositoryId == repositoryId)
                {
                    yield return entry;
                }
            }
        }

        public async Task<bool> HasSeenAsync(
            string repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            return await _gitRepoCache.CommitGraphSeen
                .FindByIdAsync(MakeRepositoryHashId(repositoryId, hash), cancellationToken)
                .ConfigureAwait(false) != null;
        }

        public async Task AddSeenAsync(
            string repositoryId,
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
            string repositoryId,
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
            string repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            await _gitRepoCache.CommitGraphFrontier
                .DeleteAsync(MakeRepositoryHashId(repositoryId, hash), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SaveRepositoryStateAsync(
            string repositoryId,
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

        public async Task DeleteTraversalEntriesAsync(string repositoryId, CancellationToken cancellationToken)
        {
            await foreach (var entry in GetFrontierAsync(repositoryId, cancellationToken).ConfigureAwait(false))
            {
                await _gitRepoCache.CommitGraphFrontier.DeleteAsync(entry.Id, cancellationToken).ConfigureAwait(false);
            }

            await foreach (var entry in GetSeenAsync(repositoryId, cancellationToken).ConfigureAwait(false))
            {
                await _gitRepoCache.CommitGraphSeen.DeleteAsync(entry.Id, cancellationToken).ConfigureAwait(false);
            }

            await _gitRepoCache.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private static string MakeRepositoryHashId(string repositoryId, string hash)
        {
            return string.Concat(repositoryId, ":", hash);
        }
    }
}
