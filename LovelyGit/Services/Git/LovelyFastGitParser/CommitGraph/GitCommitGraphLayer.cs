using System.Buffers.Binary;
using Microsoft.Win32.SafeHandles;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.CommitGraph;

internal sealed class GitCommitGraphLayer : IDisposable
{
    private const uint OidFanoutChunk = 0x4f494446;
    private const uint OidLookupChunk = 0x4f49444c;
    private const uint CommitDataChunk = 0x43444154;
    private const uint ExtraEdgesChunk = 0x45444745;
    private readonly FileStream _file;
    private readonly uint[] _fanout;
    private readonly int _hashBytes;
    private readonly long _oidLookupOffset;
    private readonly long _commitDataOffset;
    private readonly long? _extraEdgesOffset;

    private GitCommitGraphLayer(
        FileStream file,
        uint[] fanout,
        int hashBytes,
        long oidLookupOffset,
        long commitDataOffset,
        long? extraEdgesOffset,
        int baseGraphCount)
    {
        _file = file;
        _fanout = fanout;
        _hashBytes = hashBytes;
        _oidLookupOffset = oidLookupOffset;
        _commitDataOffset = commitDataOffset;
        _extraEdgesOffset = extraEdgesOffset;
        BaseGraphCount = baseGraphCount;
        LocalCount = checked((int)fanout[^1]);
    }

    public int BaseGraphCount { get; }
    public int BasePosition { get; set; }
    public int LocalCount { get; }

    public static GitCommitGraphLayer Open(string path, GitObjectFormat objectFormat)
    {
        var file = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 1,
            FileOptions.RandomAccess);
        try
        {
            Span<byte> header = stackalloc byte[8];
            ReadExactly(file.SafeFileHandle, header, 0);
            var hashVersion = objectFormat == GitObjectFormat.Sha1 ? 1 : 2;
            if (!header[..4].SequenceEqual("CGPH"u8) || header[4] != 1 || header[5] != hashVersion)
                throw new InvalidDataException("Commit-graph header is invalid.");

            var offsets = ReadChunkOffsets(file.SafeFileHandle, header[6], file.Length);
            var fanoutOffset = RequiredOffset(offsets, OidFanoutChunk);
            Span<byte> fanoutBytes = stackalloc byte[256 * sizeof(uint)];
            ReadExactly(file.SafeFileHandle, fanoutBytes, fanoutOffset);
            var fanout = new uint[256];
            for (var index = 0; index < fanout.Length; index++)
                fanout[index] = BinaryPrimitives.ReadUInt32BigEndian(fanoutBytes[(index * 4)..]);

            var hashBytes = GitObjectId.GetByteLength(objectFormat);
            var count = fanout[^1];
            var oidOffset = RequiredOffset(offsets, OidLookupChunk);
            var dataOffset = RequiredOffset(offsets, CommitDataChunk);
            ValidateRange(oidOffset, checked((long)count * hashBytes), file.Length);
            ValidateRange(dataOffset, checked((long)count * (hashBytes + 16)), file.Length);
            return new GitCommitGraphLayer(
                file,
                fanout,
                hashBytes,
                oidOffset,
                dataOffset,
                offsets.GetValueOrDefault(ExtraEdgesChunk) is var edge && edge > 0 ? edge : null,
                header[7]);
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    public int? FindLocalPosition(GitObjectId id, CancellationToken cancellationToken)
    {
        Span<byte> target = stackalloc byte[_hashBytes];
        Span<byte> current = stackalloc byte[_hashBytes];
        id.WriteTo(target);
        var bucket = target[0];
        var low = bucket == 0 ? 0u : _fanout[bucket - 1];
        var high = _fanout[bucket];
        while (low < high)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var middle = low + ((high - low) / 2);
            ReadExactly(_file.SafeFileHandle, current, _oidLookupOffset + middle * _hashBytes);
            var comparison = current.SequenceCompareTo(target);
            if (comparison < 0) low = middle + 1;
            else if (comparison > 0) high = middle;
            else return checked((int)middle);
        }
        return null;
    }

    public GitObjectId ReadId(int localPosition, GitObjectFormat format)
    {
        Span<byte> bytes = stackalloc byte[_hashBytes];
        ReadExactly(_file.SafeFileHandle, bytes, _oidLookupOffset + (long)localPosition * _hashBytes);
        return GitObjectId.FromBytes(bytes, format);
    }

    public CommitGraphData ReadData(int localPosition, GitObjectFormat format)
    {
        Span<byte> bytes = stackalloc byte[48];
        var record = bytes[..(_hashBytes + 16)];
        ReadExactly(
            _file.SafeFileHandle,
            record,
            _commitDataOffset + (long)localPosition * record.Length);
        var timeHigh = BinaryPrimitives.ReadUInt32BigEndian(record[(_hashBytes + 8)..]);
        var timeLow = BinaryPrimitives.ReadUInt32BigEndian(record[(_hashBytes + 12)..]);
        return new CommitGraphData(
            GitObjectId.FromBytes(record[.._hashBytes], format),
            BinaryPrimitives.ReadUInt32BigEndian(record[_hashBytes..]),
            BinaryPrimitives.ReadUInt32BigEndian(record[(_hashBytes + 4)..]),
            ((long)(timeHigh & 3) << 32) | timeLow);
    }

    public uint ReadExtraEdge(int index)
    {
        if (_extraEdgesOffset is not { } offset)
            throw new InvalidDataException("Commit-graph extra-edge data is missing.");
        Span<byte> bytes = stackalloc byte[4];
        ReadExactly(_file.SafeFileHandle, bytes, offset + (long)index * 4);
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    public void Dispose() => _file.Dispose();

    private static Dictionary<uint, long> ReadChunkOffsets(
        SafeFileHandle handle,
        int chunkCount,
        long fileLength)
    {
        var offsets = new Dictionary<uint, long>(chunkCount);
        var terminated = false;
        Span<byte> entry = stackalloc byte[12];
        for (var index = 0; index <= chunkCount; index++)
        {
            ReadExactly(handle, entry, 8L + index * 12L);
            var id = BinaryPrimitives.ReadUInt32BigEndian(entry);
            var offset = checked((long)BinaryPrimitives.ReadUInt64BigEndian(entry[4..]));
            if (offset < 8 || offset > fileLength) throw new InvalidDataException("Commit-graph chunk offset is invalid.");
            if (id == 0)
            {
                terminated = true;
                break;
            }
            if (!offsets.TryAdd(id, offset)) throw new InvalidDataException("Commit-graph chunk is duplicated.");
        }
        if (!terminated) throw new InvalidDataException("Commit-graph chunk table is unterminated.");
        return offsets;
    }

    private static long RequiredOffset(Dictionary<uint, long> offsets, uint id) =>
        offsets.TryGetValue(id, out var offset)
            ? offset
            : throw new InvalidDataException("Commit-graph required chunk is missing.");

    private static void ValidateRange(long offset, long length, long fileLength)
    {
        if (offset < 0 || length < 0 || offset > fileLength - length)
            throw new InvalidDataException("Commit-graph chunk is truncated.");
    }

    private static void ReadExactly(SafeFileHandle handle, Span<byte> destination, long offset)
    {
        var total = 0;
        while (total < destination.Length)
        {
            var read = RandomAccess.Read(handle, destination[total..], offset + total);
            if (read == 0) throw new EndOfStreamException("Commit-graph is truncated.");
            total += read;
        }
    }
}

internal readonly record struct CommitGraphData(
    GitObjectId TreeHash,
    uint FirstParent,
    uint SecondParent,
    long CommitUnixSeconds);
