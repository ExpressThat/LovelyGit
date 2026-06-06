using System.Buffers;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed class GitPackReader
{
    private const int PackOffsetCacheSize = 2048;

    private readonly GitObjectFormat _objectFormat;
    private readonly LruCache<PackObjectKey, GitObjectData> _packOffsetCache = new(PackOffsetCacheSize);

    public GitPackReader(GitObjectFormat objectFormat)
    {
        _objectFormat = objectFormat;
    }

    public async Task<GitObjectData> ReadObjectAtAsync(
        string packPath,
        long offset,
        Func<GitObjectId, CancellationToken, Task<GitObjectData>> readObjectAsync,
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
            var baseObject = await ReadObjectAtAsync(packPath, baseOffset, readObjectAsync, cancellationToken)
                .ConfigureAwait(false);
            return CacheAndReturn(key, new GitObjectData(
                baseObject.Kind,
                GitDeltaResolver.ApplyDelta(baseObject.Data, delta)));
        }

        if (rawKind == 7)
        {
            var baseHash = new byte[GitObjectId.GetByteLength(_objectFormat)];
            await GitPackFileHelpers.ReadExactlyAsync(file, baseHash, cancellationToken).ConfigureAwait(false);
            var baseObject = await readObjectAsync(
                    new GitObjectId(Convert.ToHexString(baseHash).ToLowerInvariant(), _objectFormat),
                    cancellationToken)
                .ConfigureAwait(false);
            var delta = await InflateRemainingAsync(file, cancellationToken).ConfigureAwait(false);
            return CacheAndReturn(key, new GitObjectData(
                baseObject.Kind,
                GitDeltaResolver.ApplyDelta(baseObject.Data, delta)));
        }

        var kind = rawKind switch
        {
            1 => GitObjectKind.Commit,
            2 => GitObjectKind.Tree,
            3 => GitObjectKind.Blob,
            4 => GitObjectKind.Tag,
            _ => throw new InvalidDataException($"Unsupported packed object kind: {rawKind}"),
        };

        return CacheAndReturn(
            key,
            new GitObjectData(kind, await InflateRemainingAsync(file, cancellationToken).ConfigureAwait(false)));
    }

    private GitObjectData CacheAndReturn(PackObjectKey key, GitObjectData objectData)
    {
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

    private readonly record struct PackObjectKey(string PackPath, long Offset);
}
