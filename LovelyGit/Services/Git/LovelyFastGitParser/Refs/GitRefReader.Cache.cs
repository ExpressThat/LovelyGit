namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static partial class GitRefReader
{
    private const int MaximumCachedRepositories = 4;
    private const int MaximumCachedRefs = 10_000;
    private static readonly object CacheLock = new();
    private static readonly Dictionary<CacheKey, LinkedListNode<CacheEntry>> Cache = new();
    private static readonly LinkedList<CacheEntry> CacheLru = new();

    public static async Task<IReadOnlyDictionary<string, GitRawRef>> LoadRefsAsync(
        string gitDirectory,
        GitObjectFormat objectFormat,
        int maxTags,
        CancellationToken cancellationToken)
    {
        var key = new CacheKey(Path.GetFullPath(gitDirectory), objectFormat, maxTags);
        var fingerprint = CreateFingerprint(key.GitDirectory);
        if (TryGetCached(key, fingerprint, out var cached))
        {
            return cached;
        }

        var refs = await LoadRefsCoreAsync(
                key.GitDirectory,
                objectFormat,
                maxTags,
                cancellationToken)
            .ConfigureAwait(false);
        if (refs.Count <= MaximumCachedRefs)
        {
            Store(key, fingerprint, refs);
        }

        return refs;
    }

    private static bool TryGetCached(
        CacheKey key,
        RefFingerprint fingerprint,
        out IReadOnlyDictionary<string, GitRawRef> refs)
    {
        lock (CacheLock)
        {
            if (Cache.TryGetValue(key, out var node) && node.Value.Fingerprint == fingerprint)
            {
                CacheLru.Remove(node);
                CacheLru.AddLast(node);
                refs = node.Value.Refs;
                return true;
            }

            if (node != null)
            {
                Cache.Remove(key);
                CacheLru.Remove(node);
            }
        }

        refs = null!;
        return false;
    }

    private static void Store(
        CacheKey key,
        RefFingerprint fingerprint,
        IReadOnlyDictionary<string, GitRawRef> refs)
    {
        lock (CacheLock)
        {
            if (Cache.Remove(key, out var existing))
            {
                CacheLru.Remove(existing);
            }

            var node = CacheLru.AddLast(new CacheEntry(key, fingerprint, refs));
            Cache.Add(key, node);
            while (Cache.Count > MaximumCachedRepositories)
            {
                var oldest = CacheLru.First!;
                CacheLru.RemoveFirst();
                Cache.Remove(oldest.Value.Key);
            }
        }
    }

    internal static RefFingerprint CreateFingerprint(
        string gitDirectory,
        string? worktreeGitDirectory = null)
    {
        var hash = new HashCode();
        AddFile(Path.Combine(worktreeGitDirectory ?? gitDirectory, "HEAD"), ref hash);
        AddFile(Path.Combine(gitDirectory, "packed-refs"), ref hash);
        AddFile(Path.Combine(gitDirectory, "logs", "refs", "stash"), ref hash);
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        var count = 0;
        if (Directory.Exists(refsDirectory))
        {
            foreach (var path in Directory.EnumerateFiles(refsDirectory, "*", SearchOption.AllDirectories))
            {
                AddFile(path, ref hash);
                count++;
            }
        }

        return new RefFingerprint(hash.ToHashCode(), count);
    }

    private static void AddFile(string path, ref HashCode hash)
    {
        if (!File.Exists(path))
        {
            hash.Add(false);
            return;
        }

        var info = new FileInfo(path);
        hash.Add(path, GetPathComparer());
        hash.Add(info.Length);
        hash.Add(info.LastWriteTimeUtc.Ticks);
    }

    private static StringComparer GetPathComparer() =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private readonly record struct CacheKey(
        string GitDirectory,
        GitObjectFormat ObjectFormat,
        int MaxTags);

    internal readonly record struct RefFingerprint(int Hash, int LooseRefCount);

    private sealed record CacheEntry(
        CacheKey Key,
        RefFingerprint Fingerprint,
        IReadOnlyDictionary<string, GitRawRef> Refs);
}
