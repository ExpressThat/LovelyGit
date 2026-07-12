namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public Task<GitCommit> GetCommitAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        return GetCommitCoreAsync(id, includeBody: true, includeDisplayText: true, _commitCache, cancellationToken);
    }

    public Task<GitCommit> GetGraphCommitAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        return GetCommitCoreAsync(id, includeBody: false, includeDisplayText: true, _graphCommitCache, cancellationToken);
    }

    public Task<GitCommit> GetGraphCommitHeaderAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        return GetCommitCoreAsync(id, includeBody: false, includeDisplayText: false, _graphHeaderCache, cancellationToken);
    }

    private async Task<GitCommit> GetCommitCoreAsync(
        GitObjectId id,
        bool includeBody,
        bool includeDisplayText,
        LruCache<GitObjectId, GitCommit> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGet(id, out var cached))
        {
            return cached;
        }

        var data = await _objectStore.ReadObjectAsync(id, includeBody, cancellationToken).ConfigureAwait(false);
        if (data.Kind != GitObjectKind.Commit)
        {
            throw new InvalidDataException($"Object is not a commit: {id}");
        }

        var commit = GitObjectParsers.ParseCommit(id, data.Data, includeBody, includeDisplayText);
        if (includeDisplayText)
        {
            AddCommitRefs(id, commit);
        }

        cache.Set(id, commit);
        return commit;
    }

    private void AddCommitRefs(GitObjectId id, GitCommit commit)
    {
        if (_refsByCommit.TryGetValue(id, out var refs))
        {
            commit.AddRefs(refs);
        }
    }
}
