using System.Buffers.Binary;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed class GitPackIndex : IDisposable
{
    private const int HeaderLength = 8;
    private const int FanoutBytes = 256 * 4;

    private readonly uint[] _fanout;
    private readonly int _hashBytes;
    private readonly FileStream _file;
    private bool _disposed;

    private GitPackIndex(string indexPath, uint[] fanout, int hashBytes, FileStream file)
    {
        IndexPath = indexPath;
        PackPath = Path.ChangeExtension(indexPath, ".pack");
        _fanout = fanout;
        _hashBytes = hashBytes;
        _file = file;
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
            FileShare.Read,
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

    private void ReadExactlyAt(Span<byte> buffer, long offset)
    {
        var filled = 0;
        while (filled < buffer.Length)
        {
            var read = RandomAccess.Read(
                _file.SafeFileHandle,
                buffer[filled..],
                offset + filled);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            filled += read;
        }
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
