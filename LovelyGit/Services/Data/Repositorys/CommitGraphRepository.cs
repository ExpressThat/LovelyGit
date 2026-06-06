using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys
{
    public class CommitGraphRepository
    {
        private readonly CommitGraphTraversalCache _traversalCache;
        private readonly CommitDetailsCacheRepository _detailsCache;
        private readonly CommitGraphRepositoryCleaner _cleaner;

        public CommitGraphRepository(GitRepoCacheDbContext gitRepoCache)
        {
            _traversalCache = new CommitGraphTraversalCache(gitRepoCache);
            _detailsCache = new CommitDetailsCacheRepository(gitRepoCache);
            _cleaner = new CommitGraphRepositoryCleaner(gitRepoCache, _traversalCache, _detailsCache);
        }

        public Task<CommitGraphRepositoryState?> GetRepositoryStateAsync(
            Guid repositoryId,
            CancellationToken cancellationToken)
        {
            return _traversalCache.GetRepositoryStateAsync(repositoryId, cancellationToken);
        }

        public IAsyncEnumerable<CommitGraphFrontierEntry> GetFrontierAsync(Guid repositoryId)
        {
            return _traversalCache.GetFrontierAsync(repositoryId);
        }

        public IAsyncEnumerable<CommitGraphSeenEntry> GetSeenAsync(Guid repositoryId)
        {
            return _traversalCache.GetSeenAsync(repositoryId);
        }

        public IAsyncEnumerable<CommitDetailsCacheEntry> GetCommitDetailsCacheEntriesAsync(Guid repositoryId)
        {
            return _detailsCache.GetCommitDetailsCacheEntriesAsync(repositoryId);
        }

        public IAsyncEnumerable<CommitChangedFileCacheEntry> GetCommitDetailsChangedFileEntriesAsync(Guid repositoryId)
        {
            return _detailsCache.GetCommitDetailsChangedFileEntriesAsync(repositoryId);
        }

        public IAsyncEnumerable<CommitGraphCachedCommitEntry> GetCachedCommitEntriesAsync(Guid repositoryId)
        {
            return _traversalCache.GetCachedCommitEntriesAsync(repositoryId);
        }

        public Task<bool> HasSeenAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            return _traversalCache.HasSeenAsync(repositoryId, hash, cancellationToken);
        }

        public Task<bool> HasCommitDetailsAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            return _detailsCache.HasCommitDetailsAsync(repositoryId, hash, cancellationToken);
        }

        public Task<List<CommitGraphCachedCommitEntry>> GetCachedCommitsAsync(
            Guid repositoryId,
            CancellationToken cancellationToken)
        {
            return _traversalCache.GetCachedCommitsAsync(repositoryId, cancellationToken);
        }

        public Task SaveCachedCommitsAsync(
            Guid repositoryId,
            CommitGraphResponse response,
            CancellationToken cancellationToken)
        {
            return _traversalCache.SaveCachedCommitsAsync(repositoryId, response, cancellationToken);
        }

        public Task<CommitDetailsResponse?> GetCommitDetailsAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            return _detailsCache.GetCommitDetailsAsync(repositoryId, hash, cancellationToken);
        }

        public Task SaveCommitDetailsAsync(
            Guid repositoryId,
            string hash,
            CommitDetailsResponse response,
            CancellationToken cancellationToken)
        {
            return _detailsCache.SaveCommitDetailsAsync(repositoryId, hash, response, cancellationToken);
        }

        public Task AddSeenAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            return _traversalCache.AddSeenAsync(repositoryId, hash, cancellationToken);
        }

        public Task AddFrontierAsync(
            Guid repositoryId,
            string hash,
            long seconds,
            CancellationToken cancellationToken)
        {
            return _traversalCache.AddFrontierAsync(repositoryId, hash, seconds, cancellationToken);
        }

        public Task DeleteFrontierAsync(
            Guid repositoryId,
            string hash,
            CancellationToken cancellationToken)
        {
            return _traversalCache.DeleteFrontierAsync(repositoryId, hash, cancellationToken);
        }

        public Task SaveRepositoryStateAsync(
            Guid repositoryId,
            int offset,
            int maxLaneCount,
            string lanes,
            CancellationToken cancellationToken)
        {
            return _traversalCache.SaveRepositoryStateAsync(
                repositoryId,
                offset,
                maxLaneCount,
                lanes,
                cancellationToken);
        }

        public Task DeleteTraversalEntriesAsync(Guid repositoryId)
        {
            return _traversalCache.DeleteTraversalEntriesAsync(repositoryId);
        }

        public Task ClearRepositoryAsync(Guid repositoryId)
        {
            return _cleaner.ClearRepositoryAsync(repositoryId);
        }
    }
}
