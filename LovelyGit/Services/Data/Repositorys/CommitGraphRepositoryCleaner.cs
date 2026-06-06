namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed class CommitGraphRepositoryCleaner
{
    private readonly GitRepoCacheDbContext _gitRepoCache;
    private readonly CommitGraphTraversalCache _traversalCache;
    private readonly CommitDetailsCacheRepository _detailsCache;

    public CommitGraphRepositoryCleaner(
        GitRepoCacheDbContext gitRepoCache,
        CommitGraphTraversalCache traversalCache,
        CommitDetailsCacheRepository detailsCache)
    {
        _gitRepoCache = gitRepoCache;
        _traversalCache = traversalCache;
        _detailsCache = detailsCache;
    }

    public async Task ClearRepositoryAsync(Guid repositoryId)
    {
        await _gitRepoCache.CommitGraphStates.DeleteAsync(repositoryId).ConfigureAwait(false);

        await foreach (var entry in _traversalCache.GetFrontierAsync(repositoryId).ConfigureAwait(false))
        {
            await _gitRepoCache.CommitGraphFrontier.DeleteAsync(entry.Id).ConfigureAwait(false);
        }

        await foreach (var entry in _traversalCache.GetSeenAsync(repositoryId).ConfigureAwait(false))
        {
            await _gitRepoCache.CommitGraphSeen.DeleteAsync(entry.Id).ConfigureAwait(false);
        }

        await foreach (var entry in _traversalCache.GetCachedCommitEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            await _gitRepoCache.CommitGraphCachedCommits.DeleteAsync(entry.Id).ConfigureAwait(false);
        }

        await foreach (var entry in _detailsCache.GetCommitDetailsCacheEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            await _gitRepoCache.CommitDetailsCache.DeleteAsync(entry.Id).ConfigureAwait(false);
        }

        await foreach (var entry in _detailsCache.GetCommitDetailsChangedFileEntriesAsync(repositoryId).ConfigureAwait(false))
        {
            await _gitRepoCache.CommitDetailsChangedFiles.DeleteAsync(entry.Id).ConfigureAwait(false);
        }

        await _gitRepoCache.SaveChangesAsync().ConfigureAwait(false);
    }
}
