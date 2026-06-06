using BLite.Core.Collections;
using BLite.Core.Transactions;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed class CommitGraphRepositoryCleaner
{
    private const int DeleteBatchSize = 250;
    private readonly GitRepoCacheDbContext _gitRepoCache;
    private readonly CommitGraphTraversalCache _traversalCache;
    private readonly CommitDetailsCacheRepository _detailsCache;
    private readonly CommitFileDiffCacheRepository _fileDiffCache;

    public CommitGraphRepositoryCleaner(
        GitRepoCacheDbContext gitRepoCache,
        CommitGraphTraversalCache traversalCache,
        CommitDetailsCacheRepository detailsCache,
        CommitFileDiffCacheRepository fileDiffCache)
    {
        _gitRepoCache = gitRepoCache;
        _traversalCache = traversalCache;
        _detailsCache = detailsCache;
        _fileDiffCache = fileDiffCache;
    }

    public async Task ClearRepositoryAsync(Guid repositoryId)
    {
        using (var transaction = _gitRepoCache.BeginTransaction())
        {
            await _gitRepoCache.CommitGraphStates.DeleteAsync(repositoryId, transaction).ConfigureAwait(false);
            await _gitRepoCache.SaveChangesAsync(transaction).ConfigureAwait(false);
        }

        await DeleteEntriesAsync(
                _gitRepoCache.CommitGraphFrontier,
                _traversalCache.GetFrontierAsync(repositoryId))
            .ConfigureAwait(false);
        await DeleteEntriesAsync(
                _gitRepoCache.CommitGraphSeen,
                _traversalCache.GetSeenAsync(repositoryId))
            .ConfigureAwait(false);
        await DeleteEntriesAsync(
                _gitRepoCache.CommitGraphCachedCommits,
                _traversalCache.GetCachedCommitEntriesAsync(repositoryId))
            .ConfigureAwait(false);
        await DeleteEntriesAsync(
                _gitRepoCache.CommitDetailsCache,
                _detailsCache.GetCommitDetailsCacheEntriesAsync(repositoryId))
            .ConfigureAwait(false);
        await DeleteEntriesAsync(
                _gitRepoCache.CommitDetailsChangedFiles,
                _detailsCache.GetCommitDetailsChangedFileEntriesAsync(repositoryId))
            .ConfigureAwait(false);
        await DeleteEntriesAsync(
                _gitRepoCache.CommitFileDiffs,
                _fileDiffCache.GetCommitFileDiffEntriesAsync(repositoryId))
            .ConfigureAwait(false);
        await DeleteEntriesAsync(
                _gitRepoCache.CommitFileDiffLines,
                _fileDiffCache.GetCommitFileDiffLineEntriesAsync(repositoryId))
            .ConfigureAwait(false);
    }

    private async Task DeleteEntriesAsync<T>(
        DocumentCollection<string, T> collection,
        IAsyncEnumerable<T> entries)
        where T : class
    {
        var ids = new List<string>(DeleteBatchSize);
        await foreach (var entry in entries.ConfigureAwait(false))
        {
            ids.Add(GetId(entry));
            if (ids.Count >= DeleteBatchSize)
            {
                await DeleteBatchAsync(collection, ids).ConfigureAwait(false);
                ids.Clear();
            }
        }

        if (ids.Count > 0)
        {
            await DeleteBatchAsync(collection, ids).ConfigureAwait(false);
        }
    }

    private async Task DeleteBatchAsync<T>(
        DocumentCollection<string, T> collection,
        IReadOnlyList<string> ids)
        where T : class
    {
        using var transaction = _gitRepoCache.BeginTransaction();
        foreach (var id in ids)
        {
            await collection.DeleteAsync(id, transaction).ConfigureAwait(false);
        }

        await _gitRepoCache.SaveChangesAsync(transaction).ConfigureAwait(false);
    }

    private static string GetId<T>(T entry)
    {
        return entry switch
        {
            Models.Git.CommitGraph.CommitGraphFrontierEntry frontier => frontier.Id,
            Models.Git.CommitGraph.CommitGraphSeenEntry seen => seen.Id,
            Models.Git.CommitGraph.CommitGraphCachedCommitEntry cachedCommit => cachedCommit.Id,
            Models.Git.CommitGraph.CommitDetailsCacheEntry details => details.Id,
            Models.Git.CommitGraph.CommitChangedFileCacheEntry changedFile => changedFile.Id,
            Models.Git.CommitGraph.CommitFileDiffCacheEntry diff => diff.Id,
            Models.Git.CommitGraph.CommitFileDiffLineCacheEntry diffLine => diffLine.Id,
            _ => throw new InvalidOperationException($"Unsupported cache entry type: {typeof(T).Name}."),
        };
    }
}
