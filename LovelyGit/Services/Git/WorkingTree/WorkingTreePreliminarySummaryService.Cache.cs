namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreePreliminarySummaryService
{
    private const int MaximumCachedRepositories = 8;
    private readonly Lock _cacheLock = new();
    private readonly Dictionary<string, CachedSummary> _summaryCache =
        new(StringComparer.OrdinalIgnoreCase);

    private int CountRootEntriesMissingFromIndexCached(
        string gitDirectory,
        string[] candidates,
        CancellationToken cancellationToken)
    {
        var indexPath = Path.Combine(gitDirectory, "index");
        var signature = ReadIndexSignature(indexPath);
        lock (_cacheLock)
        {
            if (_summaryCache.TryGetValue(indexPath, out var cached)
                && cached.Signature == signature
                && cached.Candidates.AsSpan().SequenceEqual(candidates))
            {
                return cached.MissingCount;
            }
        }

        var missingCount = CountRootEntriesMissingFromIndex(
            gitDirectory,
            candidates,
            cancellationToken);
        lock (_cacheLock)
        {
            if (_summaryCache.Count >= MaximumCachedRepositories
                && !_summaryCache.ContainsKey(indexPath))
            {
                _summaryCache.Clear();
            }

            _summaryCache[indexPath] = new CachedSummary(
                candidates,
                missingCount,
                signature);
        }

        return missingCount;
    }

    private static IndexSignature ReadIndexSignature(string indexPath)
    {
        var info = new FileInfo(indexPath);
        return info.Exists
            ? new IndexSignature(info.Length, info.LastWriteTimeUtc.Ticks)
            : default;
    }

    private sealed record CachedSummary(
        string[] Candidates,
        int MissingCount,
        IndexSignature Signature);

    private readonly record struct IndexSignature(long Length, long LastWriteTicks);
}
