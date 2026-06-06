using System.IO.Compression;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed class GitObjectStore
{
    private const int ObjectCacheSize = 512;

    private readonly string _gitDirectory;
    private readonly GitObjectFormat _objectFormat;
    private readonly GitPackReader _packReader;
    private readonly LruCache<GitObjectId, GitObjectData> _objectCache = new(ObjectCacheSize);
    private List<GitPackIndex>? _packIndexes;

    public GitObjectStore(string gitDirectory, GitObjectFormat objectFormat)
    {
        _gitDirectory = gitDirectory;
        _objectFormat = objectFormat;
        _packReader = new GitPackReader(objectFormat);
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

    private async Task<GitObjectData?> TryReadPackedObjectAsync(GitObjectId id, CancellationToken cancellationToken)
    {
        foreach (var index in await GetPackIndexesAsync(cancellationToken).ConfigureAwait(false))
        {
            var offset = await index.TryFindOffsetAsync(id, cancellationToken).ConfigureAwait(false);
            if (offset == null)
            {
                continue;
            }

            return await _packReader
                .ReadObjectAtAsync(index.PackPath, offset.Value, ReadObjectAsync, cancellationToken)
                .ConfigureAwait(false);
        }

        return null;
    }

    private async Task<IReadOnlyList<GitPackIndex>> GetPackIndexesAsync(CancellationToken cancellationToken)
    {
        if (_packIndexes != null)
        {
            return _packIndexes;
        }

        var packDirectory = Path.Combine(_gitDirectory, "objects", "pack");
        var indexes = new List<GitPackIndex>();
        if (Directory.Exists(packDirectory))
        {
            foreach (var indexPath in Directory.EnumerateFiles(packDirectory, "pack-*.idx"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                indexes.Add(await GitPackIndex.OpenAsync(indexPath, _objectFormat, cancellationToken).ConfigureAwait(false));
            }
        }

        _packIndexes = indexes;
        return indexes;
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

    private static GitObjectKind ParseKind(string value) => value switch
    {
        "commit" => GitObjectKind.Commit,
        "tree" => GitObjectKind.Tree,
        "blob" => GitObjectKind.Blob,
        "tag" => GitObjectKind.Tag,
        _ => throw new InvalidDataException($"Unsupported Git object type: {value}"),
    };
}
