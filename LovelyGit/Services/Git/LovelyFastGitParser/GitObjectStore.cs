using System.IO.Compression;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class GitObjectStore : IDisposable
{
    private const int ObjectCacheBytes = 8 * 1024 * 1024;
    private const int SharedObjectCacheSize = 4_096;

    private readonly IReadOnlyList<string> _objectDirectories;
    private readonly GitObjectFormat _objectFormat;
    private readonly GitPackReader _packReader;
    private static readonly LruCache<GitObjectId, GitObjectData> SharedObjectCache =
        new(SharedObjectCacheSize, ObjectCacheBytes, ObjectWeight);
    private readonly SemaphoreSlim _packIndexesLock = new(1, 1);
    private readonly object _packIndexSnapshotsGate = new();
    private readonly object _looseObjectIndexGate = new();
    private PackIndexSnapshot? _packIndexes;
    private HashSet<string>? _looseObjectIds;
    private bool _looseObjectIndexLoaded;
    private bool _looseObjectIndexTooLarge;

    public GitObjectStore(string gitDirectory, GitObjectFormat objectFormat)
    {
        _objectDirectories = GitAlternateObjectDirectoryReader.Read(gitDirectory);
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
        if (SharedObjectCache.TryGet(id, out var cached))
        {
            return cached;
        }

        var loose = await TryReadLooseObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (loose != null)
        {
            if (ShouldCache(cacheObject, loose))
            {
                SharedObjectCache.Set(id, loose);
            }

            return loose;
        }

        var packed = await TryReadPackedObjectAsync(id, cacheObject, cancellationToken).ConfigureAwait(false);
        if (packed != null)
        {
            if (ShouldCache(cacheObject, packed))
            {
                SharedObjectCache.Set(id, packed);
            }

            return packed;
        }

        throw new FileNotFoundException($"Git object was not found: {id}");
    }

    public async ValueTask<GitObjectData> ReadObjectWithoutCachingAsync(
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        if (SharedObjectCache.TryGet(id, out var cached))
        {
            return cached;
        }

        var loose = await TryReadLooseObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (loose != null)
        {
            return loose;
        }

        var packed = await TryReadPackedObjectAsync(id, cacheObject: false, cancellationToken).ConfigureAwait(false);
        return packed
            ?? throw new FileNotFoundException($"Git object was not found: {id}");
    }

    public async ValueTask<GitObjectData> ReadObjectWithTransientPackCacheAsync(
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        if (SharedObjectCache.TryGet(id, out var cached))
        {
            return cached;
        }

        var loose = await TryReadLooseObjectAsync(id, cancellationToken).ConfigureAwait(false);
        if (loose != null)
        {
            return loose;
        }

        var packed = await TryReadPackedObjectAsync(id, cacheObject: true, cancellationToken)
            .ConfigureAwait(false);
        return packed
            ?? throw new FileNotFoundException($"Git object was not found: {id}");
    }

    public void ClearObjectCaches()
    {
        _packReader.ClearObjectCache();
    }

    internal static bool IsSharedObjectCached(GitObjectId id)
    {
        return SharedObjectCache.TryGet(id, out _);
    }

    internal long PackObjectCacheBytes => _packReader.CachedObjectBytes;

    internal int OpenPackFileCount => _packReader.OpenPackFileCount;

    private async Task<GitObjectData?> TryReadLooseObjectAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        var value = id.Value;
        if (!MightHaveLooseObject(value))
        {
            return null;
        }

        foreach (var objectDirectory in _objectDirectories)
        {
            var path = Path.Combine(objectDirectory, value[..2], value[2..]);
            if (!File.Exists(path))
            {
                continue;
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

        return null;
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

    private static bool ShouldCache(bool cacheObject, GitObjectData data) =>
        cacheObject || data.Kind == GitObjectKind.Commit;

    public void Dispose()
    {
        lock (_packIndexSnapshotsGate)
        {
            if (Volatile.Read(ref _packIndexes) is { } indexes)
            {
                Volatile.Write(ref _packIndexes, null);
                RetirePackIndexesCore(indexes);
            }
        }
        _packIndexesLock.Dispose();
        _packReader.Dispose();
    }
}
