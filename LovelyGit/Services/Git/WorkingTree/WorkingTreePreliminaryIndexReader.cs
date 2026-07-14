using System.Buffers.Binary;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class WorkingTreePreliminaryIndexReader
{
    public static int CountMissingRootEntries(
        string indexPath,
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(indexPath))
        {
            return candidates.Count;
        }

        using var stream = new FileStream(indexPath, new FileStreamOptions
        {
            Access = FileAccess.Read,
            BufferSize = 1,
            Mode = FileMode.Open,
            Options = FileOptions.SequentialScan,
            Share = FileShare.ReadWrite | FileShare.Delete,
        });
        using var reader = new PooledSequentialReader(stream);
        Span<byte> header = stackalloc byte[12];
        reader.ReadExactly(header);
        if (!header[..4].SequenceEqual("DIRC"u8))
        {
            return candidates.Count;
        }

        var version = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(4, 4));
        if (version is not (2 or 3 or 4))
        {
            return candidates.Count;
        }

        var entries = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(8, 4));
        return CountMissingCandidates(reader, version, entries, candidates, cancellationToken);
    }

    private static int CountMissingCandidates(
        PooledSequentialReader reader,
        uint version,
        uint entries,
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken)
    {
        var missing = 0;
        var candidateIndex = 0;
        var previousPath = string.Empty;
        var previousRoot = string.Empty;
        var previousRootLength = 0;
        Span<byte> fixedBytes = stackalloc byte[62];
        Span<byte> rootBytes = stackalloc byte[256];
        Span<byte> previousRootBytes = stackalloc byte[256];
        for (var index = 0; index < entries && candidateIndex < candidates.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryStart = reader.Position;
            reader.ReadExactly(fixedBytes);
            var flags = BinaryPrimitives.ReadUInt16BigEndian(fixedBytes[60..]);
            if (version >= 3 && (flags & 0x4000) != 0)
            {
                reader.Skip(2);
            }

            string root;
            if (version == 4)
            {
                var path = ReadVersion4Path(reader, previousPath);
                previousPath = path;
                var slash = path.IndexOf('/');
                root = slash < 0 ? path : path[..slash];
            }
            else
            {
                var rootLength = ReadRootNameAndSkipPaddedPath(
                    reader,
                    entryStart,
                    flags,
                    rootBytes);
                if (rootBytes[..rootLength].SequenceEqual(previousRootBytes[..previousRootLength]))
                {
                    root = previousRoot;
                }
                else
                {
                    root = Encoding.UTF8.GetString(rootBytes[..rootLength]);
                    rootBytes[..rootLength].CopyTo(previousRootBytes);
                    previousRootLength = rootLength;
                    previousRoot = root;
                }
            }
            while (candidateIndex < candidates.Count &&
                   string.CompareOrdinal(candidates[candidateIndex], root) < 0)
            {
                missing++;
                candidateIndex++;
            }

            while (candidateIndex < candidates.Count &&
                   string.Equals(candidates[candidateIndex], root, StringComparison.Ordinal))
            {
                candidateIndex++;
            }
        }

        return missing + candidates.Count - candidateIndex;
    }

    private static string ReadVersion4Path(PooledSequentialReader reader, string previousPath)
    {
        var removeLength = ReadIndexVarInt(reader);
        var suffix = ReadNulTerminatedString(reader);
        return GitIndexPathCompression.Restore(previousPath, removeLength, suffix);
    }

    private static int ReadRootNameAndSkipPaddedPath(
        PooledSequentialReader reader,
        long entryStart,
        ushort flags,
        Span<byte> rootBytes)
    {
        var length = flags & 0x0fff;
        var rootLength = length < 0x0fff
            ? ReadRootName(reader, length, rootBytes)
            : ReadNulTerminatedRootName(reader, rootBytes);
        if (length < 0x0fff)
        {
            reader.Skip(1);
        }

        var paddedEnd = entryStart + (((reader.Position - entryStart + 7) / 8) * 8);
        reader.Skip(paddedEnd - reader.Position);
        return rootLength;
    }

    private static int ReadIndexVarInt(PooledSequentialReader reader)
    {
        var value = 0;
        int current;
        do
        {
            current = ReadRequiredByte(reader);
            value = (value << 7) + (current & 0x7f);
            if ((current & 0x80) != 0)
            {
                value++;
            }
        } while ((current & 0x80) != 0);

        return value;
    }

    private static string ReadNulTerminatedString(PooledSequentialReader reader)
    {
        var bytes = new List<byte>();
        while (ReadRequiredByte(reader) is var value && value != 0)
        {
            bytes.Add((byte)value);
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static int ReadRootName(
        PooledSequentialReader reader,
        int pathLength,
        Span<byte> rootBytes)
    {
        Span<byte> pathBytes = stackalloc byte[0x0fff];
        var path = pathBytes[..pathLength];
        reader.ReadExactly(path);
        var slash = path.IndexOf((byte)'/');
        var rootLength = Math.Min(slash < 0 ? pathLength : slash, rootBytes.Length);
        path[..rootLength].CopyTo(rootBytes);
        return rootLength;
    }

    private static int ReadNulTerminatedRootName(
        PooledSequentialReader reader,
        Span<byte> rootBytes)
    {
        var rootLength = 0;
        while (true)
        {
            var value = ReadRequiredByte(reader);
            if (value == 0)
            {
                return rootLength;
            }

            if (value == '/' || rootLength >= rootBytes.Length)
            {
                while (ReadRequiredByte(reader) != 0)
                {
                }

                return rootLength;
            }

            rootBytes[rootLength++] = (byte)value;
        }
    }

    private static int ReadRequiredByte(PooledSequentialReader reader)
    {
        var value = reader.ReadByte();
        return value >= 0 ? value : throw new EndOfStreamException();
    }
}
