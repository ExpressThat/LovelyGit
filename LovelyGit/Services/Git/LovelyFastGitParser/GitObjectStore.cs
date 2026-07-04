using System.IO.Compression;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class GitObjectStore : IDisposable
{
    private const int ObjectCacheSize = 256;
    private const int ObjectCacheBytes = 8 * 1024 * 1024;

    private readonly string _gitDirectory;
    private readonly GitObjectFormat _objectFormat;
    private readonly GitPackReader _packReader;
    private readonly LruCache<GitObjectId, GitObjectData> _objectCache =
        new(ObjectCacheSize, ObjectCacheBytes, ObjectWeight);
    private readonly SemaphoreSlim _packIndexesLock = new(1, 1);
    private readonly object _looseObjectIndexGate = new();
    private List<GitPackIndex>? _packIndexes;
    private readonly List<GitPackIndex> _retiredPackIndexes = [];
    private HashSet<string>? _looseObjectIds;
    private bool _looseObjectIndexLoaded;
    private bool _looseObjectIndexTooLarge;

    public GitObjectStore(string gitDirectory, GitObjectFormat objectFormat)
    {
        _gitDirectory = gitDirectory;
        _objectFormat = objectFormat;
        _packReader = new GitPackReader(objectFormat);
    }

    public async Task<GitObjectData> ReadObjectAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        return await ReadObjectAsync(id, cacheObject: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitObjectData> ReadObjectAsync(
        GitObjectId id,
        bool cacheObject,
        CancellationToken cancellationToken)
    {
        if (_objectCache.TryGet(id, out var cached))
        {
            return cached;
        }

        var loose = await TryReadLooseObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (loose != null)
        {
            if (cacheObject)
            {
                _objectCache.Set(id, loose);
            }

            return loose;
        }

        var packed = await TryReadPackedObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (packed != null)
        {
            if (cacheObject)
            {
                _objectCache.Set(id, packed);
            }

            return packed;
        }

        throw new FileNotFoundException($"Git object was not found: {id}");
    }

    public void ClearObjectCaches()
    {
        _objectCache.Clear();
        _packReader.ClearObjectCache();
    }

    private async Task<GitObjectData?> TryReadLooseObjectAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        var value = id.Value;
        var path = Path.Combine(_gitDirectory, "objects", value[..2], value[2..]);
        if (!MightHaveLooseObject(value) || !File.Exists(path))
        {
            return null;
        }

        await using var file = File.OpenRead(path);
        await using var zlib = new ZLibStream(file, CompressionMode.Decompress);
        using var inflated = new MemoryStream();
        await zlib.CopyToAsync(inflated, cancellationToken).ConfigureAwait(false);
        if (inflated.TryGetBuffer(out var buffer))
        {
            return ParseLooseObject(buffer.AsSpan(0, checked((int)inflated.Length)));
        }

        return ParseLooseObject(inflated.ToArray());
    }

    private async Task<GitObjectData?> TryReadPackedObjectAsync(
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var result = await TryReadPackedObjectFromCurrentIndexesAsync(id, cancellationToken).ConfigureAwait(false);
            if (result.ObjectData != null)
            {
                return result.ObjectData;
            }

            if (!result.PackIndexesStale)
            {
                return null;
            }

            await ReloadPackIndexesAsync(cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private async Task<PackedObjectReadResult> TryReadPackedObjectFromCurrentIndexesAsync(
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        foreach (var index in await GetPackIndexesAsync(cancellationToken).ConfigureAwait(false))
        {
            var offset = index.TryFindOffset(id, cancellationToken);
            if (offset == null)
            {
                continue;
            }

            if (!File.Exists(index.PackPath))
            {
                return new PackedObjectReadResult(null, PackIndexesStale: true);
            }

            var objectData = await _packReader
                .ReadObjectAtAsync(
                    index.PackPath,
                    offset.Value,
                    ReadObjectAsync,
                    cancellationToken)
                .ConfigureAwait(false);

            return new PackedObjectReadResult(objectData, PackIndexesStale: false);
        }

        return new PackedObjectReadResult(null, PackIndexesStale: false);
    }

    private async Task<IReadOnlyList<GitPackIndex>> GetPackIndexesAsync(CancellationToken cancellationToken)
    {
        var cachedIndexes = Volatile.Read(ref _packIndexes);
        if (cachedIndexes != null)
        {
            return cachedIndexes;
        }

        await _packIndexesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cachedIndexes = Volatile.Read(ref _packIndexes);
            if (cachedIndexes != null)
            {
                return cachedIndexes;
            }

            var packDirectory = Path.Combine(_gitDirectory, "objects", "pack");
            var indexes = new List<GitPackIndex>();
            if (Directory.Exists(packDirectory))
            {
                foreach (var indexPath in Directory.EnumerateFiles(packDirectory, "*.idx"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!File.Exists(indexPath) || !File.Exists(Path.ChangeExtension(indexPath, ".pack")))
                    {
                        continue;
                    }

                    indexes.Add(await GitPackIndex.OpenAsync(indexPath, _objectFormat, cancellationToken).ConfigureAwait(false));
                }
            }

            Volatile.Write(ref _packIndexes, indexes);
            return indexes;
        }
        finally
        {
            _packIndexesLock.Release();
        }
    }

    private async Task ReloadPackIndexesAsync(CancellationToken cancellationToken)
    {
        await _packIndexesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _packIndexes) is { } indexes)
            {
                _retiredPackIndexes.AddRange(indexes);
            }

            _packReader.ClearObjectCache();
            Volatile.Write(ref _packIndexes, null);
        }
        finally
        {
            _packIndexesLock.Release();
        }
    }

    private static GitObjectData ParseLooseObject(ReadOnlySpan<byte> inflated)
    {
        var zeroIndex = inflated.IndexOf((byte)0);
        if (zeroIndex <= 0)
        {
            throw new InvalidDataException("Loose object has no header terminator.");
        }

        var header = inflated[..zeroIndex];
        var spaceIndex = header.IndexOf((byte)' ');
        if (spaceIndex <= 0)
        {
            throw new InvalidDataException("Loose object header is invalid.");
        }

        var kind = ParseKind(header[..spaceIndex]);
        var data = inflated[(zeroIndex + 1)..].ToArray();
        return new GitObjectData(kind, data);
    }

    private static GitObjectKind ParseKind(ReadOnlySpan<byte> value)
    {
        if (value.SequenceEqual("commit"u8))
        {
            return GitObjectKind.Commit;
        }

        if (value.SequenceEqual("tree"u8))
        {
            return GitObjectKind.Tree;
        }

        if (value.SequenceEqual("blob"u8))
        {
            return GitObjectKind.Blob;
        }

        if (value.SequenceEqual("tag"u8))
        {
            return GitObjectKind.Tag;
        }

        throw new InvalidDataException($"Unsupported Git object type: {System.Text.Encoding.ASCII.GetString(value)}");
    }

    private static long ObjectWeight(GitObjectData data)
    {
        return data.Data.LongLength;
    }

    private readonly record struct PackedObjectReadResult(GitObjectData? ObjectData, bool PackIndexesStale);

    public void Dispose()
    {
        _objectCache.Clear();
        if (Volatile.Read(ref _packIndexes) is { } indexes)
        {
            foreach (var index in indexes)
            {
                index.Dispose();
            }
        }

        foreach (var index in _retiredPackIndexes)
        {
            index.Dispose();
        }

        _retiredPackIndexes.Clear();
        Volatile.Write(ref _packIndexes, null);
        _packIndexesLock.Dispose();
        _packReader.Dispose();
    }
}
