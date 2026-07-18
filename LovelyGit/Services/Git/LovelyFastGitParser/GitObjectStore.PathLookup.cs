using System.Buffers;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class GitObjectStore
{
    public async Task<GitTreeFile?> TryGetTreeFileAsync(
        GitObjectId treeId,
        string normalizedPath,
        CancellationToken cancellationToken)
    {
        var nameBuffer = ArrayPool<byte>.Shared.Rent(
            Encoding.UTF8.GetMaxByteCount(normalizedPath.Length));
        try
        {
            var byteCount = Encoding.UTF8.GetBytes(normalizedPath, nameBuffer);
            return await TryGetTreeFileAsync(
                    treeId,
                    normalizedPath,
                    nameBuffer.AsMemory(0, byteCount),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(nameBuffer);
        }
    }

    internal async Task<GitTreeFile?> TryGetTreeFileAsync(
        GitObjectId treeId,
        string normalizedPath,
        ReadOnlyMemory<byte> encodedPath,
        CancellationToken cancellationToken)
    {
        var currentTree = treeId;
        var segmentStart = 0;
        while (segmentStart < encodedPath.Length)
        {
            var relativeSlash = encodedPath.Span[segmentStart..].IndexOf((byte)'/');
            var segmentLength = relativeSlash < 0
                ? encodedPath.Length - segmentStart
                : relativeSlash;
            var data = await ReadObjectAsync(currentTree, cancellationToken).ConfigureAwait(false);
            if (!GitObjectParsers.TryFindTreeEntry(
                    currentTree,
                    _objectFormat,
                    data,
                    encodedPath.Span.Slice(segmentStart, segmentLength),
                    out var entry))
            {
                return null;
            }

            if (relativeSlash < 0)
            {
                return entry.IsTree
                    ? null
                    : new GitTreeFile(normalizedPath, entry.ObjectId, entry.Mode);
            }
            if (!entry.IsTree) return null;
            currentTree = entry.ObjectId;
            segmentStart += segmentLength + 1;
        }
        return null;
    }
}
