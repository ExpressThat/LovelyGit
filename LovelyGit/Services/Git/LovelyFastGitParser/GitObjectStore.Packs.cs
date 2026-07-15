using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class GitObjectStore
{
    private async ValueTask<GitObjectData?> TryReadPackedObjectAsync(
        GitObjectId id,
        bool cacheObject,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var result = await TryReadPackedObjectFromCurrentIndexesAsync(id, cacheObject, cancellationToken)
                .ConfigureAwait(false);
            if (result.ObjectData != null)
            {
                return result.ObjectData;
            }

            if (!result.PackIndexesStale)
            {
                return null;
            }

            await ReloadPackIndexesAsync(result.PackIndexGeneration, cancellationToken)
                .ConfigureAwait(false);
        }

        return null;
    }

    private async ValueTask<PackedObjectReadResult> TryReadPackedObjectFromCurrentIndexesAsync(
        GitObjectId id,
        bool cacheObject,
        CancellationToken cancellationToken)
    {
        using var indexes = await AcquirePackIndexesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var index in indexes.Indexes)
        {
            var offset = index.TryFindOffset(id, cancellationToken);
            if (offset == null)
            {
                continue;
            }

            if (!File.Exists(index.PackPath))
            {
                return new PackedObjectReadResult(
                    null,
                    PackIndexesStale: true,
                    indexes.Generation);
            }

            var objectData = await _packReader
                .ReadObjectAtAsync(
                    index.PackPath,
                    offset.Value,
                    cacheObject
                        ? (objectId, token) => new ValueTask<GitObjectData>(ReadObjectAsync(objectId, token))
                        : ReadObjectWithoutCachingAsync,
                    cacheObject,
                    cancellationToken)
                .ConfigureAwait(false);
            return new PackedObjectReadResult(
                objectData,
                PackIndexesStale: false,
                indexes.Generation);
        }

        return new PackedObjectReadResult(null, PackIndexesStale: false, indexes.Generation);
    }

    private async ValueTask<PackIndexLease> AcquirePackIndexesAsync(
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var cachedIndexes = Volatile.Read(ref _packIndexes);
            if (cachedIndexes == null)
            {
                await LoadPackIndexesAsync(cancellationToken).ConfigureAwait(false);
                continue;
            }

            lock (_packIndexSnapshotsGate)
            {
                if (ReferenceEquals(cachedIndexes, Volatile.Read(ref _packIndexes)) &&
                    !cachedIndexes.Retired)
                {
                    cachedIndexes.ActiveReaders++;
                    return new PackIndexLease(this, cachedIndexes);
                }
            }
        }
    }

    private async Task LoadPackIndexesAsync(CancellationToken cancellationToken)
    {
        await _packIndexesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _packIndexes) != null)
            {
                return;
            }

            var indexes = new List<GitPackIndex>();
            try
            {
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
            }
            catch
            {
                foreach (var index in indexes) index.Dispose();
                throw;
            }

            lock (_packIndexSnapshotsGate)
            {
                Interlocked.Add(ref _openPackIndexCount, indexes.Count);
                Volatile.Write(ref _packIndexes, new PackIndexSnapshot(
                    Interlocked.Increment(ref _nextPackIndexGeneration),
                    indexes));
            }
        }
        finally
        {
            _packIndexesLock.Release();
        }
    }

    private async Task ReloadPackIndexesAsync(
        long staleGeneration,
        CancellationToken cancellationToken)
    {
        await _packIndexesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            lock (_packIndexSnapshotsGate)
            {
                if (Volatile.Read(ref _packIndexes) is not { } indexes ||
                    indexes.Generation != staleGeneration)
                {
                    return;
                }

                Volatile.Write(ref _packIndexes, null);
                RetirePackIndexesCore(indexes);
            }
        }
        finally
        {
            _packIndexesLock.Release();
        }
    }

    private readonly record struct PackedObjectReadResult(
        GitObjectData? ObjectData,
        bool PackIndexesStale,
        long PackIndexGeneration);
}
