using System.Buffers.Binary;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreePreliminarySummaryService
{
    public Task<WorkingTreeChangeSummaryResponse> GetSummaryAsync(
        string workTreeDirectory,
        string gitDirectory,
        CancellationToken cancellationToken)
    {
        var candidates = Directory.EnumerateFileSystemEntries(workTreeDirectory)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !name.Equals(".git", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
        {
            return Task.FromResult(Incomplete(0));
        }

        var count = CountRootEntriesMissingFromIndexCached(gitDirectory, candidates, cancellationToken);
        return Task.FromResult(Incomplete(count));
    }

    private static int CountRootEntriesMissingFromIndex(
        string gitDirectory,
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken)
    {
        var indexPath = Path.Combine(gitDirectory, "index");
        if (!File.Exists(indexPath))
        {
            return candidates.Count;
        }

        using var stream = new FileStream(indexPath, new FileStreamOptions
        {
            Access = FileAccess.Read,
            BufferSize = 64 * 1024,
            Mode = FileMode.Open,
            Options = FileOptions.SequentialScan,
            Share = FileShare.ReadWrite | FileShare.Delete,
        });
        Span<byte> header = stackalloc byte[12];
        stream.ReadExactly(header);
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
        return CountMissingCandidates(stream, version, entries, hashLength: 20, candidates, cancellationToken);
    }

    private static int CountMissingCandidates(
        Stream stream,
        uint version,
        uint entries,
        int hashLength,
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken)
    {
        var missing = 0;
        var candidateIndex = 0;
        var previousPath = string.Empty;
        Span<byte> fixedBytes = stackalloc byte[96];
        for (var index = 0; index < entries && candidateIndex < candidates.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryStart = stream.Position;
            var fixedLength = 42 + hashLength;
            stream.ReadExactly(fixedBytes[..fixedLength]);
            var flags = BinaryPrimitives.ReadUInt16BigEndian(fixedBytes.Slice(40 + hashLength, 2));
            if (version >= 3 && (flags & 0x4000) != 0)
            {
                stream.Position += 2;
            }

            var pathStart = stream.Position;
            var path = version == 4
                ? ReadVersion4Path(stream, previousPath)
                : ReadRootNameAndSkipPaddedPath(stream, entryStart, pathStart, flags);
            previousPath = path;
            var slash = path.IndexOf('/');
            var root = slash < 0 ? path : path[..slash];
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

    private static string ReadVersion4Path(Stream stream, string previousPath)
    {
        var prefixLength = ReadIndexVarInt(stream);
        var suffix = ReadNulTerminatedString(stream);
        return string.Concat(previousPath.AsSpan(0, prefixLength), suffix);
    }

    private static string ReadRootNameAndSkipPaddedPath(Stream stream, long entryStart, long pathStart, ushort flags)
    {
        var length = flags & 0x0fff;
        var path = length < 0x0fff
            ? ReadRootName(stream, length)
            : ReadNulTerminatedRootName(stream);
        if (length < 0x0fff)
        {
            stream.Position = pathStart + length + 1;
        }

        stream.Position = entryStart + (((stream.Position - entryStart + 7) / 8) * 8);
        return path;
    }

    private static int ReadIndexVarInt(Stream stream)
    {
        var value = 0;
        int current;
        do
        {
            current = stream.ReadByte();
            if (current < 0)
            {
                throw new EndOfStreamException();
            }

            value = (value << 7) + (current & 0x7f);
            if ((current & 0x80) != 0)
            {
                value++;
            }
        } while ((current & 0x80) != 0);

        return value;
    }

    private static string ReadNulTerminatedString(Stream stream)
    {
        var bytes = new List<byte>();
        while (true)
        {
            var value = stream.ReadByte();
            if (value < 0)
            {
                throw new EndOfStreamException();
            }

            if (value == 0)
            {
                return Encoding.UTF8.GetString(bytes.ToArray());
            }

            bytes.Add((byte)value);
        }
    }

    private static string ReadRootName(Stream stream, int pathLength)
    {
        Span<byte> rootBytes = stackalloc byte[256];
        var rootLength = 0;
        for (var index = 0; index < pathLength; index++)
        {
            var value = stream.ReadByte();
            if (value < 0)
            {
                throw new EndOfStreamException();
            }

            if (value == '/' || rootLength >= rootBytes.Length)
            {
                stream.Position += pathLength - index - 1;
                break;
            }

            rootBytes[rootLength++] = (byte)value;
        }

        return Encoding.UTF8.GetString(rootBytes[..rootLength]);
    }

    private static string ReadNulTerminatedRootName(Stream stream)
    {
        Span<byte> rootBytes = stackalloc byte[256];
        var rootLength = 0;
        while (true)
        {
            var value = stream.ReadByte();
            if (value < 0)
            {
                throw new EndOfStreamException();
            }

            if (value == 0)
            {
                return Encoding.UTF8.GetString(rootBytes[..rootLength]);
            }

            if (value == '/' || rootLength >= rootBytes.Length)
            {
                while (stream.ReadByte() is > 0)
                {
                }

                return Encoding.UTF8.GetString(rootBytes[..rootLength]);
            }

            rootBytes[rootLength++] = (byte)value;
        }
    }

    private static WorkingTreeChangeSummaryResponse Incomplete(int totalCount) => new()
    {
        IsComplete = false,
        TotalCount = totalCount,
    };
}
