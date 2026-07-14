using ICSharpCode.SharpZipLib.Zip.Compression;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal sealed partial class GitPackReader : IDisposable
{
    private const int PackOffsetCacheSize = 4_096;
    private const int PackOffsetCacheBytes = 8 * 1024 * 1024;

    private readonly GitObjectFormat _objectFormat;
    private readonly LruCache<PackObjectKey, GitObjectData> _packOffsetCache =
        new(PackOffsetCacheSize, PackOffsetCacheBytes, ObjectWeight);
    private readonly object _packFilesGate = new();
    private readonly Dictionary<string, FileStream> _packFiles = new(StringComparer.Ordinal);

    public GitPackReader(GitObjectFormat objectFormat)
    {
        _objectFormat = objectFormat;
    }

    public async ValueTask<GitObjectData> ReadObjectAtAsync(
        string packPath,
        long offset,
        Func<GitObjectId, CancellationToken, ValueTask<GitObjectData>> readObjectAsync,
        bool cacheObject,
        CancellationToken cancellationToken)
    {
        var key = new PackObjectKey(packPath, offset);
        if (_packOffsetCache.TryGet(key, out var cached))
        {
            return cached;
        }

        using var file = new RandomAccessPackStream(GetPackFile(packPath).SafeFileHandle, offset);

        var first = file.ReadByte();
        if (first < 0)
        {
            throw new EndOfStreamException();
        }

        var rawKind = (first >> 4) & 0x07;
        var objectSize = ReadVariableSize(file, first);

        if (rawKind == 6)
        {
            var baseOffset = offset - ReadOffsetDeltaBaseDistance(file);
            var delta = InflateRemaining(file, objectSize, cancellationToken);
            var baseObject = await ReadObjectAtAsync(
                    packPath,
                    baseOffset,
                    readObjectAsync,
                    cacheObject,
                    cancellationToken)
                .ConfigureAwait(false);
            return Return(key, new GitObjectData(
                baseObject.Kind,
                GitDeltaResolver.ApplyDelta(baseObject.Data, delta)), cacheObject);
        }

        if (rawKind == 7)
        {
            Span<byte> baseHash = stackalloc byte[GitObjectId.GetByteLength(_objectFormat)];
            GitPackFileHelpers.ReadExactly(file, baseHash, cancellationToken);
            var baseObject = await readObjectAsync(
                    GitObjectId.FromBytes(baseHash, _objectFormat),
                    cancellationToken)
                .ConfigureAwait(false);
            var delta = InflateRemaining(file, objectSize, cancellationToken);
            return Return(key, new GitObjectData(
                baseObject.Kind,
                GitDeltaResolver.ApplyDelta(baseObject.Data, delta)), cacheObject);
        }

        var kind = rawKind switch
        {
            1 => GitObjectKind.Commit,
            2 => GitObjectKind.Tree,
            3 => GitObjectKind.Blob,
            4 => GitObjectKind.Tag,
            _ => throw new InvalidDataException($"Unsupported packed object kind: {rawKind}"),
        };

        return Return(
            key,
            new GitObjectData(kind, InflateRemaining(file, objectSize, cancellationToken)),
            cacheObject);
    }

    private GitObjectData Return(PackObjectKey key, GitObjectData objectData, bool cacheObject)
    {
        if (cacheObject) _packOffsetCache.Set(key, objectData);
        return objectData;
    }

    internal long CachedObjectBytes => _packOffsetCache.CurrentWeight;

    public void ClearObjectCache()
    {
        _packOffsetCache.Clear();
    }

    public void ClearPackFiles()
    {
        ClearObjectCache();
        lock (_packFilesGate)
        {
            foreach (var file in _packFiles.Values)
            {
                file.Dispose();
            }

            _packFiles.Clear();
        }
    }

    public void Dispose()
    {
        ClearPackFiles();
    }

    private FileStream GetPackFile(string packPath)
    {
        lock (_packFilesGate)
        {
            if (_packFiles.TryGetValue(packPath, out var file))
            {
                return file;
            }

            file = new FileStream(
                packPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 1,
                FileOptions.RandomAccess);
            _packFiles.Add(packPath, file);
            return file;
        }
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

    private static long ObjectWeight(GitObjectData data)
    {
        return data.Data.LongLength;
    }

    private readonly record struct PackObjectKey(string PackPath, long Offset);
}
