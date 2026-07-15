using System.Buffers;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public async Task<GitTreeFile?> TryGetTreeFileAsync(
        GitObjectId treeId,
        string normalizedPath,
        CancellationToken cancellationToken)
    {
        var nameBuffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(normalizedPath.Length));
        try
        {
            var currentTree = treeId;
            var segmentStart = 0;
            while (segmentStart < normalizedPath.Length)
            {
                var slash = normalizedPath.IndexOf('/', segmentStart);
                var segmentEnd = slash < 0 ? normalizedPath.Length : slash;
                var byteCount = Encoding.UTF8.GetBytes(
                    normalizedPath.AsSpan(segmentStart, segmentEnd - segmentStart),
                    nameBuffer);
                var data = await _objectStore
                    .ReadObjectAsync(currentTree, cancellationToken)
                    .ConfigureAwait(false);
                if (!GitObjectParsers.TryFindTreeEntry(
                        currentTree,
                        ObjectFormat,
                        data,
                        nameBuffer.AsSpan(0, byteCount),
                        out var entry))
                {
                    return null;
                }

                if (slash < 0)
                {
                    return entry.IsTree
                        ? null
                        : new GitTreeFile(normalizedPath, entry.ObjectId, entry.Mode);
                }

                if (!entry.IsTree)
                {
                    return null;
                }

                currentTree = entry.ObjectId;
                segmentStart = slash + 1;
            }

            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(nameBuffer);
        }
    }

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
