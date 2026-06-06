using System.Buffers.Binary;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed class GitPackIndex
{
    private const int HeaderLength = 8;
    private const int FanoutBytes = 256 * 4;

    private readonly uint[] _fanout;
    private readonly int _hashBytes;

    private GitPackIndex(string indexPath, uint[] fanout, int hashBytes)
    {
        IndexPath = indexPath;
        PackPath = Path.ChangeExtension(indexPath, ".pack");
        _fanout = fanout;
        _hashBytes = hashBytes;
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
        await using var file = File.OpenRead(indexPath);
        var header = new byte[HeaderLength];
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

        return new GitPackIndex(indexPath, fanout, GitObjectId.GetByteLength(objectFormat));
    }

    public async Task<long?> TryFindOffsetAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        var target = id.ToByteArray();
        var bucket = target[0];
        var low = bucket == 0 ? 0 : _fanout[bucket - 1];
        var high = _fanout[bucket];
        if (low >= high)
        {
            return null;
        }

        await using var file = File.OpenRead(IndexPath);
        var currentHash = new byte[_hashBytes];
        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            file.Seek(HashTableOffset + mid * _hashBytes, SeekOrigin.Begin);
            await GitPackFileHelpers.ReadExactlyAsync(file, currentHash, cancellationToken).ConfigureAwait(false);
            var comparison = currentHash.AsSpan().SequenceCompareTo(target);
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
                return await ReadObjectOffsetAsync(file, mid, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
    }

    private async Task<long> ReadObjectOffsetAsync(FileStream file, uint objectIndex, CancellationToken cancellationToken)
    {
        file.Seek(OffsetTableOffset + objectIndex * 4, SeekOrigin.Begin);
        var small = new byte[4];
        await GitPackFileHelpers.ReadExactlyAsync(file, small, cancellationToken).ConfigureAwait(false);
        var offset = BinaryPrimitives.ReadUInt32BigEndian(small);
        if ((offset & 0x80000000U) == 0)
        {
            return offset;
        }

        var largeIndex = offset & 0x7fffffffU;
        file.Seek(LargeOffsetTableOffset + largeIndex * 8, SeekOrigin.Begin);
        var buffer = new byte[8];
        await GitPackFileHelpers.ReadExactlyAsync(file, buffer, cancellationToken).ConfigureAwait(false);
        var largeOffset = BinaryPrimitives.ReadUInt64BigEndian(buffer);
        if (largeOffset > long.MaxValue)
        {
            throw new InvalidDataException("Pack offset exceeds Int64 range.");
        }

        return (long)largeOffset;
    }
}
