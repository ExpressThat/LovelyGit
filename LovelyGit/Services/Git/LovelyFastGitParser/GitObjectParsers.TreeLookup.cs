using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal static partial class GitObjectParsers
{
    public static bool TryFindTreeEntry(
        GitObjectId treeId,
        GitObjectFormat objectFormat,
        GitObjectData data,
        ReadOnlySpan<byte> expectedName,
        out GitTreePathEntry entry)
    {
        if (data.Kind != GitObjectKind.Tree)
        {
            throw new InvalidDataException($"Object is not a tree: {treeId}");
        }

        var source = data.Data.AsSpan();
        var index = 0;
        while (index < source.Length)
        {
            var modeStart = index;
            var modeEnd = source[index..].IndexOf((byte)' ');
            if (modeEnd < 0)
            {
                throw new InvalidDataException("Tree entry has no mode terminator.");
            }

            modeEnd += index;
            index = modeEnd + 1;
            var nameStart = index;
            var nameEnd = source[index..].IndexOf((byte)0);
            if (nameEnd < 0)
            {
                throw new InvalidDataException("Tree entry has no name terminator.");
            }

            nameEnd += index;
            index = nameEnd + 1;
            var hashLength = GitObjectId.GetByteLength(objectFormat);
            if (index + hashLength > source.Length)
            {
                throw new InvalidDataException("Tree entry object id is truncated.");
            }

            if (source[nameStart..nameEnd].SequenceEqual(expectedName))
            {
                entry = new GitTreePathEntry(
                    GitObjectId.FromBytes(source.Slice(index, hashLength), objectFormat),
                    Encoding.ASCII.GetString(source[modeStart..modeEnd]));
                return true;
            }

            index += hashLength;
        }

        entry = default;
        return false;
    }
}
