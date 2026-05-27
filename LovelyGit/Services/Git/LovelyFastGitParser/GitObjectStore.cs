using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed class GitObjectStore
{
    private const int ObjectCacheSize = 512;
    private const int PackOffsetCacheSize = 2048;

    private readonly string _gitDirectory;
    private readonly GitObjectFormat _objectFormat;
    private readonly LruCache<GitObjectId, GitObjectData> _objectCache = new(ObjectCacheSize);
    private readonly LruCache<PackObjectKey, GitObjectData> _packOffsetCache = new(PackOffsetCacheSize);
    private List<PackIndex>? _packIndexes;

    public GitObjectStore(string gitDirectory, GitObjectFormat objectFormat)
    {
        _gitDirectory = gitDirectory;
        _objectFormat = objectFormat;
    }

    public async Task<GitObjectData> ReadObjectAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        if (_objectCache.TryGet(id, out var cached))
        {
            return cached;
        }

        var loose = await TryReadLooseObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (loose != null)
        {
            _objectCache.Set(id, loose);
            return loose;
        }

        var packed = await TryReadPackedObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (packed != null)
        {
            _objectCache.Set(id, packed);
            return packed;
        }

        throw new FileNotFoundException($"Git object was not found: {id}");
    }

    private async Task<GitObjectData?> TryReadLooseObjectAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        var value = id.Value;
        var path = Path.Combine(_gitDirectory, "objects", value[..2], value[2..]);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var file = File.OpenRead(path);
        await using var zlib = new ZLibStream(file, CompressionMode.Decompress);
        using var inflated = new MemoryStream();
        await zlib.CopyToAsync(inflated, cancellationToken).ConfigureAwait(false);
        return ParseLooseObject(inflated.ToArray());
    }

    private static GitObjectData ParseLooseObject(byte[] inflated)
    {
        var zeroIndex = Array.IndexOf(inflated, (byte)0);
        if (zeroIndex <= 0)
        {
            throw new InvalidDataException("Loose object has no header terminator.");
        }

        var header = System.Text.Encoding.ASCII.GetString(inflated, 0, zeroIndex);
        var spaceIndex = header.IndexOf(' ');
        if (spaceIndex <= 0)
        {
            throw new InvalidDataException("Loose object header is invalid.");
        }

        var kind = ParseKind(header[..spaceIndex]);
        var data = new byte[inflated.Length - zeroIndex - 1];
        Buffer.BlockCopy(inflated, zeroIndex + 1, data, 0, data.Length);
        return new GitObjectData(kind, data);
    }

    private async Task<GitObjectData?> TryReadPackedObjectAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        foreach (var index in await GetPackIndexesAsync(cancellationToken).ConfigureAwait(false))
        {
            var offset = await index.TryFindOffsetAsync(id, cancellationToken).ConfigureAwait(false);
            if (offset == null)
            {
                continue;
            }

            return await ReadPackObjectAtAsync(index.PackPath, offset.Value, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private async Task<IReadOnlyList<PackIndex>> GetPackIndexesAsync(CancellationToken cancellationToken)
    {
        if (_packIndexes != null)
        {
            return _packIndexes;
        }

        var packDirectory = Path.Combine(_gitDirectory, "objects", "pack");
        var indexes = new List<PackIndex>();
        if (Directory.Exists(packDirectory))
        {
            foreach (var indexPath in Directory.EnumerateFiles(packDirectory, "pack-*.idx"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                indexes.Add(await PackIndex.OpenAsync(indexPath, _objectFormat, cancellationToken).ConfigureAwait(false));
            }
        }

        _packIndexes = indexes;
        return indexes;
    }

    private async Task<GitObjectData> ReadPackObjectAtAsync(
        string packPath,
        long offset,
        CancellationToken cancellationToken)
    {
        var key = new PackObjectKey(packPath, offset);
        if (_packOffsetCache.TryGet(key, out var cached))
        {
            return cached;
        }

        await using var file = File.OpenRead(packPath);
        file.Seek(offset, SeekOrigin.Begin);

        var first = file.ReadByte();
        if (first < 0)
        {
            throw new EndOfStreamException();
        }

        var rawKind = (first >> 4) & 0x07;
        _ = ReadVariableSize(file, first);

        if (rawKind == 6)
        {
            var baseOffset = offset - ReadOffsetDeltaBaseDistance(file);
            var delta = await InflateRemainingAsync(file, cancellationToken).ConfigureAwait(false);
            var baseObject = await ReadPackObjectAtAsync(packPath, baseOffset, cancellationToken).ConfigureAwait(false);
            var resolved = new GitObjectData(baseObject.Kind, ApplyDelta(baseObject.Data, delta));
            _packOffsetCache.Set(key, resolved);
            return resolved;
        }

        if (rawKind == 7)
        {
            var baseHash = new byte[GitObjectId.GetByteLength(_objectFormat)];
            await ReadExactlyAsync(file, baseHash, cancellationToken).ConfigureAwait(false);
            var baseObject = await ReadObjectAsync(new GitObjectId(Convert.ToHexString(baseHash).ToLowerInvariant(), _objectFormat), cancellationToken)
                .ConfigureAwait(false);
            var delta = await InflateRemainingAsync(file, cancellationToken).ConfigureAwait(false);
            var resolved = new GitObjectData(baseObject.Kind, ApplyDelta(baseObject.Data, delta));
            _packOffsetCache.Set(key, resolved);
            return resolved;
        }

        var kind = rawKind switch
        {
            1 => GitObjectKind.Commit,
            2 => GitObjectKind.Tree,
            3 => GitObjectKind.Blob,
            4 => GitObjectKind.Tag,
            _ => throw new InvalidDataException($"Unsupported packed object kind: {rawKind}"),
        };

        var objectData = new GitObjectData(kind, await InflateRemainingAsync(file, cancellationToken).ConfigureAwait(false));
        _packOffsetCache.Set(key, objectData);
        return objectData;
    }

    private static ulong ReadVariableSize(Stream stream, int first)
    {
        ulong value = (uint)(first & 0x0f);
        var shift = 4;
        var current = first;
        while ((current & 0x80) != 0)
        {
            current = stream.ReadByte();
            if (current < 0)
            {
                throw new EndOfStreamException();
            }

            value |= ((ulong)(current & 0x7f)) << shift;
            shift += 7;
        }

        return value;
    }

    private static long ReadOffsetDeltaBaseDistance(Stream stream)
    {
        var current = stream.ReadByte();
        if (current < 0)
        {
            throw new EndOfStreamException();
        }

        long value = current & 0x7f;
        while ((current & 0x80) != 0)
        {
            current = stream.ReadByte();
            if (current < 0)
            {
                throw new EndOfStreamException();
            }

            value = ((value + 1) << 7) | (byte)(current & 0x7f);
        }

        return value;
    }

    private static async Task<byte[]> InflateRemainingAsync(Stream stream, CancellationToken cancellationToken)
    {
        var inflater = new Inflater(noHeader: false);
        var input = ArrayPool<byte>.Shared.Rent(8192);
        var output = ArrayPool<byte>.Shared.Rent(8192);
        using var inflated = new MemoryStream();
        try
        {
            while (!inflater.IsFinished)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (inflater.IsNeedingInput)
                {
                    var read = await stream.ReadAsync(input.AsMemory(0, input.Length), cancellationToken)
                        .ConfigureAwait(false);
                    if (read == 0)
                    {
                        throw new EndOfStreamException("Packed object zlib stream ended unexpectedly.");
                    }

                    inflater.SetInput(input, 0, read);
                }

                var written = inflater.Inflate(output, 0, output.Length);
                if (written > 0)
                {
                    inflated.Write(output, 0, written);
                    continue;
                }

                if (inflater.IsNeedingDictionary)
                {
                    throw new InvalidDataException("Packed object requires an unsupported zlib dictionary.");
                }

                if (!inflater.IsNeedingInput && !inflater.IsFinished)
                {
                    throw new InvalidDataException("Packed object zlib stream made no progress.");
                }
            }

            return inflated.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(input);
            ArrayPool<byte>.Shared.Return(output);
        }
    }

    private static byte[] ApplyDelta(byte[] baseData, byte[] delta)
    {
        var index = 0;
        var sourceLength = ReadDeltaVarInt(delta, ref index);
        if (sourceLength != (ulong)baseData.Length)
        {
            throw new InvalidDataException("Delta source length does not match base object length.");
        }

        var resultLength = ReadDeltaVarInt(delta, ref index);
        using var result = resultLength <= int.MaxValue
            ? new MemoryStream((int)resultLength)
            : new MemoryStream();

        while (index < delta.Length)
        {
            var opcode = delta[index++];
            if ((opcode & 0x80) != 0)
            {
                var copyOffset = 0;
                var copyLength = 0;

                if ((opcode & 0x01) != 0) copyOffset |= delta[index++];
                if ((opcode & 0x02) != 0) copyOffset |= delta[index++] << 8;
                if ((opcode & 0x04) != 0) copyOffset |= delta[index++] << 16;
                if ((opcode & 0x08) != 0) copyOffset |= delta[index++] << 24;
                if ((opcode & 0x10) != 0) copyLength |= delta[index++];
                if ((opcode & 0x20) != 0) copyLength |= delta[index++] << 8;
                if ((opcode & 0x40) != 0) copyLength |= delta[index++] << 16;
                if (copyLength == 0) copyLength = 0x10000;

                result.Write(baseData, copyOffset, copyLength);
            }
            else if (opcode != 0)
            {
                result.Write(delta, index, opcode);
                index += opcode;
            }
            else
            {
                throw new InvalidDataException("Invalid zero delta opcode.");
            }
        }

        var bytes = result.ToArray();
        if ((ulong)bytes.Length != resultLength)
        {
            throw new InvalidDataException("Delta result length does not match expected length.");
        }

        return bytes;
    }

    private static ulong ReadDeltaVarInt(byte[] data, ref int index)
    {
        ulong value = 0;
        var shift = 0;
        while (true)
        {
            if (index >= data.Length)
            {
                throw new InvalidDataException("Delta varint overran buffer.");
            }

            var current = data[index++];
            value |= ((ulong)(current & 0x7f)) << shift;
            if ((current & 0x80) == 0)
            {
                return value;
            }

            shift += 7;
        }
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var readTotal = 0;
        while (readTotal < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(readTotal), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            readTotal += read;
        }
    }

    private static GitObjectKind ParseKind(string value) => value switch
    {
        "commit" => GitObjectKind.Commit,
        "tree" => GitObjectKind.Tree,
        "blob" => GitObjectKind.Blob,
        "tag" => GitObjectKind.Tag,
        _ => throw new InvalidDataException($"Unsupported Git object type: {value}"),
    };

    private sealed class PackIndex
    {
        private const int HeaderLength = 8;
        private const int FanoutBytes = 256 * 4;

        private readonly uint[] _fanout;
        private readonly int _hashBytes;

        private PackIndex(string indexPath, uint[] fanout, int hashBytes)
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

        public static async Task<PackIndex> OpenAsync(
            string indexPath,
            GitObjectFormat objectFormat,
            CancellationToken cancellationToken)
        {
            await using var file = File.OpenRead(indexPath);
            var header = new byte[HeaderLength];
            await ReadExactlyAsync(file, header, cancellationToken).ConfigureAwait(false);
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
            await ReadExactlyAsync(file, fanoutBytes, cancellationToken).ConfigureAwait(false);
            var fanout = new uint[256];
            for (var i = 0; i < fanout.Length; i++)
            {
                fanout[i] = BinaryPrimitives.ReadUInt32BigEndian(fanoutBytes.AsSpan(i * 4));
            }

            return new PackIndex(indexPath, fanout, GitObjectId.GetByteLength(objectFormat));
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
                await ReadExactlyAsync(file, currentHash, cancellationToken).ConfigureAwait(false);
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
            await ReadExactlyAsync(file, small, cancellationToken).ConfigureAwait(false);
            var offset = BinaryPrimitives.ReadUInt32BigEndian(small);
            if ((offset & 0x80000000U) == 0)
            {
                return offset;
            }

            var largeIndex = offset & 0x7fffffffU;
            file.Seek(LargeOffsetTableOffset + largeIndex * 8, SeekOrigin.Begin);
            var buffer = new byte[8];
            await ReadExactlyAsync(file, buffer, cancellationToken).ConfigureAwait(false);
            var largeOffset = BinaryPrimitives.ReadUInt64BigEndian(buffer);
            if (largeOffset > long.MaxValue)
            {
                throw new InvalidDataException("Pack offset exceeds Int64 range.");
            }

            return (long)largeOffset;
        }
    }

    private readonly record struct PackObjectKey(string PackPath, long Offset);
}
