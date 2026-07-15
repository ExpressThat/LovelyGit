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
            var currentTree = treeId;
            var segmentStart = 0;
            while (segmentStart < normalizedPath.Length)
            {
                var slash = normalizedPath.IndexOf('/', segmentStart);
                var segmentEnd = slash < 0 ? normalizedPath.Length : slash;
                var byteCount = Encoding.UTF8.GetBytes(
                    normalizedPath.AsSpan(segmentStart, segmentEnd - segmentStart),
                    nameBuffer);
                var data = await ReadObjectAsync(currentTree, cancellationToken)
                    .ConfigureAwait(false);
                if (!GitObjectParsers.TryFindTreeEntry(
                        currentTree,
                        _objectFormat,
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
}
