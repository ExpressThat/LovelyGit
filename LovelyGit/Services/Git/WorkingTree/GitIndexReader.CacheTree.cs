using System.Buffers.Binary;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIndexReader
{
    private static GitObjectId? TryReadCacheTreeRootId(
        byte[] indexBytes,
        int extensionOffset,
        int hashLength,
        GitObjectFormat objectFormat)
    {
        var checksumOffset = indexBytes.Length - hashLength;
        while (extensionOffset + 8 <= checksumOffset)
        {
            var signature = indexBytes.AsSpan(extensionOffset, 4);
            var rawSize = BinaryPrimitives.ReadUInt32BigEndian(indexBytes.AsSpan(extensionOffset + 4, 4));
            if (rawSize > int.MaxValue)
            {
                return null;
            }

            var size = (int)rawSize;
            extensionOffset += 8;
            if (extensionOffset + size > checksumOffset)
            {
                return null;
            }

            if (signature.SequenceEqual("TREE"u8))
            {
                return TryReadCacheTreeRootId(indexBytes.AsSpan(extensionOffset, size), hashLength, objectFormat);
            }

            extensionOffset += size;
        }

        return null;
    }

    private static GitObjectId? TryReadCacheTreeRootId(
        ReadOnlySpan<byte> cacheTree,
        int hashLength,
        GitObjectFormat objectFormat)
    {
        var pathEnd = cacheTree.IndexOf((byte)0);
        if (pathEnd != 0)
        {
            return null;
        }

        var lineEnd = cacheTree[(pathEnd + 1)..].IndexOf((byte)'\n');
        if (lineEnd < 0)
        {
            return null;
        }

        var objectOffset = pathEnd + 1 + lineEnd + 1;
        if (objectOffset + hashLength > cacheTree.Length || cacheTree[pathEnd + 1] == (byte)'-')
        {
            return null;
        }

        return new GitObjectId(
            Convert.ToHexString(cacheTree.Slice(objectOffset, hashLength)).ToLowerInvariant(),
            objectFormat);
    }
}

internal sealed record GitIndexSnapshot(
    IReadOnlyList<GitIndexEntry> Entries,
    GitObjectId? RootTreeId);
