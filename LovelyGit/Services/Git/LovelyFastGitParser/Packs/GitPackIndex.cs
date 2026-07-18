using System.Buffers.Binary;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed partial class GitPackIndex : IDisposable
{
    private const int HeaderLength = 8;
    private const int FanoutBytes = 256 * 4;

    private readonly uint[] _fanout;
    private readonly int _hashBytes;
    private readonly FileStream _file;
    private readonly long _fileLength;
    private bool _disposed;

    private GitPackIndex(string indexPath, uint[] fanout, int hashBytes, FileStream file)
    {
        IndexPath = indexPath;
        PackPath = Path.ChangeExtension(indexPath, ".pack");
        _fanout = fanout;
        _hashBytes = hashBytes;
        _file = file;
        _fileLength = file.Length;
    }

    public string IndexPath { get; }
    public string PackPath { get; }
    private uint Count => _fanout[255];
    private long HashTableOffset => HeaderLength + FanoutBytes;
    private long CrcTableOffset => HashTableOffset + Count * _hashBytes;
    private long OffsetTableOffset => CrcTableOffset + Count * 4;
    private long LargeOffsetTableOffset => OffsetTableOffset + Count * 4;

    public static async Task<GitPackIndex> OpenAsync(
        string indexPath,
        GitObjectFormat objectFormat,
        CancellationToken cancellationToken)
    {
        var file = new FileStream(
            indexPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 1,
            FileOptions.RandomAccess);
        var header = new byte[HeaderLength];
        try
        {
            await GitPackFileHelpers.ReadExactlyAsync(file, header, cancellationToken).ConfigureAwait(false);
            if (header[0] != 0xff || header[1] != 0x74 || header[2] != 0x4f || header[3] != 0x63)
            {
                throw new InvalidDataException("Only Git pack index v2 files are supported.");
            }

            var version = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4));
            if (version != 2)
            {
                throw new InvalidDataException("Only Git pack index v2 files are supported.");
            }

            var fanoutBytes = new byte[FanoutBytes];
            await GitPackFileHelpers.ReadExactlyAsync(file, fanoutBytes, cancellationToken).ConfigureAwait(false);
            var fanout = new uint[256];
            for (var i = 0; i < fanout.Length; i++)
            {
                fanout[i] = BinaryPrimitives.ReadUInt32BigEndian(fanoutBytes.AsSpan(i * 4));
            }

            return new GitPackIndex(indexPath, fanout, GitObjectId.GetByteLength(objectFormat), file);
        }
        catch
        {
            await file.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public long? TryFindOffset(GitObjectId id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Span<byte> target = stackalloc byte[_hashBytes];
        Span<byte> currentHash = stackalloc byte[_hashBytes];
        id.WriteTo(target);
        var bucket = target[0];
        var low = bucket == 0 ? 0 : _fanout[bucket - 1];
        var high = _fanout[bucket];
        if (low >= high)
        {
            return null;
        }

        while (low < high)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mid = low + ((high - low) / 2);
            ReadExactlyAt(currentHash, HashTableOffset + mid * _hashBytes);
            var comparison = currentHash.SequenceCompareTo(target);
            if (comparison < 0)
            {
                low = mid + 1;
            }
            else if (comparison > 0)
            {
                high = mid;
            }
            else
            {
                return ReadObjectOffset(mid);
            }
        }

        return null;
    }

    public void AddIdsWithPrefix(
        string prefix,
        GitObjectFormat objectFormat,
        ISet<GitObjectId> results,
        int maximumResults,
        CancellationToken cancellationToken)
    {
        if (prefix.Length < 2 || maximumResults <= 0) return;
        Span<byte> target = stackalloc byte[_hashBytes];
        WritePrefix(prefix, target);
        var bucket = target[0];
        var start = bucket == 0 ? 0 : _fanout[bucket - 1];
        var end = _fanout[bucket];
        Span<byte> hash = stackalloc byte[_hashBytes];
        var first = FindLowerBound(target, start, end, hash, cancellationToken);
        for (var index = first; index < end && results.Count < maximumResults; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadExactlyAt(hash, HashTableOffset + index * _hashBytes);
            if (!MatchesPrefix(hash, prefix)) break;
            results.Add(GitObjectId.FromBytes(hash, objectFormat));
        }
    }

    private uint FindLowerBound(
        ReadOnlySpan<byte> target,
        uint low,
        uint high,
        Span<byte> current,
        CancellationToken cancellationToken)
    {
        while (low < high)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var middle = low + ((high - low) / 2);
            ReadExactlyAt(current, HashTableOffset + middle * _hashBytes);
            if (current.SequenceCompareTo(target) < 0) low = middle + 1;
            else high = middle;
        }
        return low;
    }

    private static void WritePrefix(ReadOnlySpan<char> prefix, Span<byte> destination)
    {
        destination.Clear();
        for (var index = 0; index < prefix.Length; index++)
        {
            var nibble = prefix[index] <= '9'
                ? prefix[index] - '0'
                : (prefix[index] | 0x20) - 'a' + 10;
            if ((index & 1) == 0) destination[index / 2] = (byte)(nibble << 4);
            else destination[index / 2] |= (byte)nibble;
        }
    }

    private static bool MatchesPrefix(ReadOnlySpan<byte> hash, ReadOnlySpan<char> prefix)
    {
        for (var index = 0; index < prefix.Length; index++)
        {
            var nibble = (index & 1) == 0 ? hash[index / 2] >> 4 : hash[index / 2] & 0x0f;
            var expected = prefix[index] <= '9'
                ? prefix[index] - '0'
                : (prefix[index] | 0x20) - 'a' + 10;
            if (nibble != expected) return false;
        }
        return true;
    }

    private long ReadObjectOffset(uint objectIndex)
    {
        Span<byte> small = stackalloc byte[4];
        ReadExactlyAt(small, OffsetTableOffset + objectIndex * 4);
        var offset = BinaryPrimitives.ReadUInt32BigEndian(small);
        if ((offset & 0x80000000U) == 0)
        {
            return offset;
        }

        var largeIndex = offset & 0x7fffffffU;
        Span<byte> buffer = stackalloc byte[8];
        ReadExactlyAt(buffer, LargeOffsetTableOffset + largeIndex * 8);
        var largeOffset = BinaryPrimitives.ReadUInt64BigEndian(buffer);
        if (largeOffset > long.MaxValue)
        {
            throw new InvalidDataException("Pack offset exceeds Int64 range.");
        }

        return (long)largeOffset;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _file.Dispose();
        _disposed = true;
    }
}
