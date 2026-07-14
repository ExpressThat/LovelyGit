using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class GitIndexReader
{
    public Task<IReadOnlyList<GitIndexEntry>> ReadEntriesForPathAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        string path,
        CancellationToken cancellationToken)
    {
        var indexPath = Path.Combine(gitDirectory, "index");
        if (!File.Exists(indexPath))
        {
            return Task.FromResult<IReadOnlyList<GitIndexEntry>>([]);
        }

        cancellationToken.ThrowIfCancellationRequested();
        path = NormalizePath(path);
        using var stream = new FileStream(indexPath, new FileStreamOptions
        {
            Access = FileAccess.Read,
            BufferSize = 1,
            Mode = FileMode.Open,
            Options = FileOptions.SequentialScan,
            Share = FileShare.ReadWrite | FileShare.Delete,
        });
        using var reader = new PooledSequentialReader(stream);
        var entries = ReadEntriesForPath(reader, objectFormat, path, cancellationToken);
        return Task.FromResult<IReadOnlyList<GitIndexEntry>>(entries);
    }

    private static List<GitIndexEntry> ReadEntriesForPath(
        PooledSequentialReader reader,
        GitObjectFormat objectFormat,
        string targetPath,
        CancellationToken cancellationToken)
    {
        Span<byte> header = stackalloc byte[12];
        reader.ReadExactly(header);
        if (!header[..4].SequenceEqual("DIRC"u8))
        {
            throw new InvalidDataException("Git index header is invalid.");
        }

        var version = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(4, 4));
        if (version is not (2 or 3 or 4))
        {
            throw new NotSupportedException($"Unsupported Git index version: {version}");
        }

        var count = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(8, 4));
        var target = Encoding.UTF8.GetBytes(targetPath);
        var pathBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(256, target.Length + 1));
        try
        {
            return ReadEntries(reader, objectFormat, version, count, targetPath, target, ref pathBuffer, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pathBuffer);
        }
    }

    private static List<GitIndexEntry> ReadEntries(
        PooledSequentialReader reader,
        GitObjectFormat objectFormat,
        uint version,
        uint count,
        string targetPath,
        byte[] target,
        ref byte[] pathBuffer,
        CancellationToken cancellationToken)
    {
        var matches = new List<GitIndexEntry>(3);
        var hashLength = GitObjectId.GetByteLength(objectFormat);
        var fixedLength = 40 + hashLength + 2;
        Span<byte> fixedBytes = stackalloc byte[74];
        Span<byte> extended = stackalloc byte[2];
        var previousPathLength = 0;
        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryStart = reader.Position;
            var entryBytes = fixedBytes[..fixedLength];
            reader.ReadExactly(entryBytes);
            var flags = BinaryPrimitives.ReadUInt16BigEndian(entryBytes[^2..]);
            ushort extendedFlags = 0;
            if (version >= 3 && (flags & 0x4000) != 0)
            {
                reader.ReadExactly(extended);
                extendedFlags = BinaryPrimitives.ReadUInt16BigEndian(extended);
            }

            var pathLength = version == 4
                ? ReadVersion4Path(reader, ref pathBuffer, previousPathLength)
                : ReadPaddedPath(reader, ref pathBuffer, flags, entryStart);
            previousPathLength = pathLength;
            var comparison = pathBuffer.AsSpan(0, pathLength).SequenceCompareTo(target);
            if (comparison > 0) break;
            if (comparison != 0) continue;

            matches.Add(CreateEntry(targetPath, objectFormat, entryBytes, flags, extendedFlags));
        }

        return matches;
    }

    private static GitIndexEntry CreateEntry(
        string path,
        GitObjectFormat objectFormat,
        ReadOnlySpan<byte> bytes,
        ushort flags,
        ushort extendedFlags)
    {
        var hashLength = GitObjectId.GetByteLength(objectFormat);
        var objectId = Convert.ToHexString(bytes.Slice(40, hashLength)).ToLowerInvariant();
        return new GitIndexEntry(
            path,
            new GitObjectId(objectId, objectFormat),
            ToTreeMode(BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(24, 4))),
            (flags >> 12) & 0x3,
            BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(36, 4)),
            DateTimeOffset.FromUnixTimeSeconds(BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(8, 4))),
            (flags & 0x8000) != 0,
            (extendedFlags & 0x4000) != 0,
            (extendedFlags & 0x2000) != 0);
    }

    private static int ReadPaddedPath(
        PooledSequentialReader reader,
        ref byte[] pathBuffer,
        ushort flags,
        long entryStart)
    {
        var length = flags & 0x0fff;
        var pathLength = length < 0x0fff
            ? ReadKnownPath(reader, ref pathBuffer, length)
            : ReadPathSuffix(reader, ref pathBuffer, 0);
        var paddedEnd = entryStart + (((reader.Position - entryStart + 7) / 8) * 8);
        reader.Skip(paddedEnd - reader.Position);
        return pathLength;
    }

    private static int ReadKnownPath(PooledSequentialReader reader, ref byte[] pathBuffer, int length)
    {
        EnsureCapacity(ref pathBuffer, length);
        reader.ReadExactly(pathBuffer.AsSpan(0, length));
        if (reader.ReadByte() != 0) throw new InvalidDataException("Git index path is unterminated.");
        return length;
    }

    private static int ReadVersion4Path(
        PooledSequentialReader reader,
        ref byte[] pathBuffer,
        int previousLength)
    {
        var remove = ReadIndexVarInt(reader);
        if (remove > previousLength) throw new InvalidDataException("Git index v4 path prefix is invalid.");
        return ReadPathSuffix(reader, ref pathBuffer, previousLength - remove);
    }

    private static int ReadPathSuffix(PooledSequentialReader reader, ref byte[] pathBuffer, int offset)
    {
        while (true)
        {
            var value = reader.ReadByte();
            if (value < 0) throw new EndOfStreamException();
            if (value == 0) return offset;
            EnsureCapacity(ref pathBuffer, offset + 1);
            pathBuffer[offset++] = (byte)value;
        }
    }

    private static int ReadIndexVarInt(PooledSequentialReader reader)
    {
        var value = 0;
        int current;
        do
        {
            current = reader.ReadByte();
            if (current < 0) throw new EndOfStreamException();
            value = (value << 7) + (current & 0x7f);
            if ((current & 0x80) != 0) value++;
        } while ((current & 0x80) != 0);
        return value;
    }

    private static void EnsureCapacity(ref byte[] buffer, int length)
    {
        if (length <= buffer.Length) return;
        var replacement = ArrayPool<byte>.Shared.Rent(Math.Max(length, buffer.Length * 2));
        buffer.AsSpan().CopyTo(replacement);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = replacement;
    }
}
