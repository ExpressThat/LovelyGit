namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public async Task<GitTreeFile?> TryGetTreeFileAsync(
        GitObjectId treeId,
        string normalizedPath,
        CancellationToken cancellationToken) =>
        await _objectStore.TryGetTreeFileAsync(treeId, normalizedPath, cancellationToken)
            .ConfigureAwait(false);

    public async Task<GitCommitTraversalHeader> GetCommitTraversalHeaderAsync(
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        var data = await _objectStore
            .ReadObjectWithTransientPackCacheAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (data.Kind != GitObjectKind.Commit)
        {
            throw new InvalidDataException($"Object is not a commit: {id}");
        }

        return GitObjectParsers.ParseCommitTraversalHeader(id, data.Data);
    }

    public async Task<GitCommitAncestryHeader> GetCommitAncestryHeaderAsync(
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        if (GetCommitGraph()?.TryRead(id, cancellationToken, out var graphHeader) == true)
            return graphHeader;
        var data = await _objectStore
            .ReadObjectWithTransientPackCacheAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (data.Kind != GitObjectKind.Commit)
            throw new InvalidDataException($"Object is not a commit: {id}");
        return GitObjectParsers.ParseCommitAncestryHeader(id, data.Data);
    }

    public async Task<GitTreeFile?> FindTreeFileByObjectIdAsync(
        GitObjectId treeId,
        GitObjectId objectId,
        CancellationToken cancellationToken) =>
        await FindTreeFileByObjectIdAsync(treeId, objectId, string.Empty, cancellationToken)
            .ConfigureAwait(false);

    private async Task<GitTreeFile?> FindTreeFileByObjectIdAsync(
        GitObjectId treeId,
        GitObjectId objectId,
        string prefix,
        CancellationToken cancellationToken)
    {
        foreach (var entry in await ReadTreeEntriesAsync(treeId, prefix, cancellationToken)
                     .ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.IsTree)
            {
                var nested = await FindTreeFileByObjectIdAsync(
                        entry.ObjectId, objectId, entry.Path, cancellationToken)
                    .ConfigureAwait(false);
                if (nested != null)
                {
                    return nested;
                }
            }
            else if (entry.ObjectId == objectId)
            {
                return new GitTreeFile(entry.Path, entry.ObjectId, entry.Mode);
            }
        }

        return null;
    }
}
