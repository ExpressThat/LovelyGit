using AutoDependencyRegistration.Attributes;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys
{
    [RegisterClassAsSingleton]
    public class CommitGraphRepository
    {
        private readonly AppDbContext _appContext;

        public CommitGraphRepository(AppDbContext appContext)
        {
            _appContext = appContext;
        }

        public Task<CommitGraphRepositoryState?> GetRepositoryStateAsync(
            string repositoryId,
            CancellationToken cancellationToken)
        {
            return _appContext.CommitGraphStates.FindByIdAsync(repositoryId, cancellationToken).AsTask();
        }

        public async IAsyncEnumerable<CommitGraphFrontierEntry> GetFrontierAsync(
            string repositoryId,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var entry in _appContext.CommitGraphFrontier.FindAllAsync(cancellationToken)
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
            await foreach (var entry in _appContext.CommitGraphSeen.FindAllAsync(cancellationToken)
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
            return await _appContext.CommitGraphSeen
                .FindByIdAsync(MakeRepositoryHashId(repositoryId, hash), cancellationToken)
                .ConfigureAwait(false) != null;
        }

        public async Task AddSeenAsync(
            string repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            await _appContext.CommitGraphSeen.InsertAsync(
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
            await _appContext.CommitGraphFrontier.InsertAsync(
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
            await _appContext.CommitGraphFrontier
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
                await _appContext.CommitGraphStates.InsertAsync(state, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _appContext.CommitGraphStates.UpdateAsync(state, cancellationToken).ConfigureAwait(false);
            }

            await _appContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteRepositoryGraphAsync(string repositoryId, CancellationToken cancellationToken)
        {
            await _appContext.CommitGraphStates.DeleteAsync(repositoryId, cancellationToken).ConfigureAwait(false);
            await DeleteTraversalEntriesAsync(repositoryId, cancellationToken).ConfigureAwait(false);

            await _appContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task ClearTransientGraphStateAsync(CancellationToken cancellationToken)
        {
            await foreach (var state in _appContext.CommitGraphStates.FindAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                await _appContext.CommitGraphStates.DeleteAsync(state.Id, cancellationToken)
                    .ConfigureAwait(false);
            }

            await foreach (var entry in _appContext.CommitGraphFrontier.FindAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                await _appContext.CommitGraphFrontier.DeleteAsync(entry.Id, cancellationToken)
                    .ConfigureAwait(false);
            }

            await foreach (var entry in _appContext.CommitGraphSeen.FindAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                await _appContext.CommitGraphSeen.DeleteAsync(entry.Id, cancellationToken)
                    .ConfigureAwait(false);
            }

            await _appContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteTraversalEntriesAsync(string repositoryId, CancellationToken cancellationToken)
        {
            await foreach (var entry in GetFrontierAsync(repositoryId, cancellationToken).ConfigureAwait(false))
            {
                await _appContext.CommitGraphFrontier.DeleteAsync(entry.Id, cancellationToken).ConfigureAwait(false);
            }

            await foreach (var entry in GetSeenAsync(repositoryId, cancellationToken).ConfigureAwait(false))
            {
                await _appContext.CommitGraphSeen.DeleteAsync(entry.Id, cancellationToken).ConfigureAwait(false);
            }

            await _appContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private static string MakeRepositoryHashId(string repositoryId, string hash)
        {
            return string.Concat(repositoryId, ":", hash);
        }
    }
}
