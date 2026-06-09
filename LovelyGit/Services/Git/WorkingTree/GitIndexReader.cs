using System.Buffers.Binary;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class GitIndexReader
{
    public async Task<IReadOnlyList<GitIndexEntry>> ReadAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var indexPath = Path.Combine(gitDirectory, "index");
        if (!File.Exists(indexPath))
        {
            return Array.Empty<GitIndexEntry>();
        }

        var bytes = await File.ReadAllBytesAsync(indexPath, cancellationToken).ConfigureAwait(false);
        if (bytes.Length < 12 || !bytes.AsSpan(0, 4).SequenceEqual("DIRC"u8))
        {
            throw new InvalidDataException("Git index header is invalid.");
        }

        var version = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(4, 4));
        if (version is not (2 or 3 or 4))
        {
            throw new NotSupportedException($"Unsupported Git index version: {version}");
        }

        var count = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8, 4));
        var hashLength = GitObjectId.GetByteLength(objectFormat);
        var entries = new List<GitIndexEntry>(checked((int)Math.Min(count, int.MaxValue)));
        var offset = 12;
        var previousPath = string.Empty;

        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (offset + 62 + hashLength - 20 > bytes.Length)
            {
                throw new InvalidDataException("Git index entry is truncated.");
            }

            var entryStart = offset;
            var ctimeSeconds = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 8;
            var mtimeSeconds = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 8;
            var dev = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;
            var ino = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;
            var mode = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;
            var uid = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;
            var gid = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;
            var fileSize = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;

            var objectId = new GitObjectId(
                Convert.ToHexString(bytes.AsSpan(offset, hashLength)).ToLowerInvariant(),
                objectFormat);
            offset += hashLength;

            var flags = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            offset += 2;
            ushort extendedFlags = 0;
            if (version >= 3 && (flags & 0x4000) != 0)
            {
                extendedFlags = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
                offset += 2;
            }

            string path;
            if (version == 4)
            {
                var prefixLength = ReadIndexVarInt(bytes, ref offset);
                var end = Array.IndexOf(bytes, (byte)0, offset);
                if (end < 0)
                {
                    throw new InvalidDataException("Git index v4 path is unterminated.");
                }

                var suffix = Encoding.UTF8.GetString(bytes, offset, end - offset);
                offset = end + 1;
                path = string.Concat(previousPath.AsSpan(0, prefixLength), suffix);
            }
            else
            {
                var nameLength = flags & 0x0fff;
                if (nameLength < 0x0fff)
                {
                    path = Encoding.UTF8.GetString(bytes, offset, nameLength);
                    offset += nameLength + 1;
                }
                else
                {
                    var end = Array.IndexOf(bytes, (byte)0, offset);
                    if (end < 0)
                    {
                        throw new InvalidDataException("Git index path is unterminated.");
                    }

                    path = Encoding.UTF8.GetString(bytes, offset, end - offset);
                    offset = end + 1;
                }

                var paddedLength = ((offset - entryStart + 7) / 8) * 8;
                offset = entryStart + paddedLength;
            }

            previousPath = path;
            entries.Add(new GitIndexEntry(
                NormalizePath(path),
                objectId,
                ToTreeMode(mode),
                (flags >> 12) & 0x3,
                fileSize,
                DateTimeOffset.FromUnixTimeSeconds(mtimeSeconds),
                (flags & 0x8000) != 0,
                (extendedFlags & 0x4000) != 0,
                (extendedFlags & 0x2000) != 0));

            _ = ctimeSeconds;
            _ = dev;
            _ = ino;
            _ = uid;
            _ = gid;
        }

        return entries;
    }

    private static int ReadIndexVarInt(byte[] bytes, ref int offset)
    {
        var value = 0;
        byte current;
        do
        {
            if (offset >= bytes.Length)
            {
                throw new InvalidDataException("Git index varint is truncated.");
            }

            current = bytes[offset++];
            value = (value << 7) + (current & 0x7f);
            if ((current & 0x80) != 0)
            {
                value++;
            }
        } while ((current & 0x80) != 0);

        return value;
    }

    private static string ToTreeMode(uint indexMode)
    {
        return Convert.ToString(indexMode & 0xffff, 8);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}

internal sealed record GitIndexEntry(
    string Path,
    GitObjectId ObjectId,
    string Mode,
    int Stage,
    uint FileSize,
    DateTimeOffset ModifiedTime,
    bool AssumeUnchanged,
    bool SkipWorkTree,
    bool IntentToAdd);
