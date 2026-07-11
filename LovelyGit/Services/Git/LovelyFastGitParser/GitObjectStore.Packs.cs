using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class GitObjectStore
{
    private async Task<GitObjectData?> TryReadPackedObjectAsync(
        GitObjectId id,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var result = await TryReadPackedObjectFromCurrentIndexesAsync(id, cancellationToken)
                .ConfigureAwait(false);
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

    private async Task<IReadOnlyList<GitPackIndex>> GetPackIndexesAsync(
        CancellationToken cancellationToken)
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

            var indexes = new List<GitPackIndex>();
            foreach (var objectDirectory in _objectDirectories)
            {
                var packDirectory = Path.Combine(objectDirectory, "pack");
                if (!Directory.Exists(packDirectory))
                {
                    continue;
                }

                foreach (var indexPath in Directory.EnumerateFiles(packDirectory, "*.idx"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!File.Exists(indexPath) ||
                        !File.Exists(Path.ChangeExtension(indexPath, ".pack")))
                    {
                        continue;
                    }

                    indexes.Add(
                        await GitPackIndex
                            .OpenAsync(indexPath, _objectFormat, cancellationToken)
                            .ConfigureAwait(false));
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

    private readonly record struct PackedObjectReadResult(
        GitObjectData? ObjectData,
        bool PackIndexesStale);
}
