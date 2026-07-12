using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class ConflictResolutionResponseCache(int capacity = 8)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedListNode<Entry>> _entries = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> _lru = new();

    internal int Count
    {
        get
        {
            lock (_gate) return _entries.Count;
        }
    }

    public bool TryGet(
        string repositoryPath,
        string path,
        string fingerprint,
        bool ignoreWhitespace,
        out ConflictResolutionResponse response)
    {
        var key = Key(repositoryPath, path, fingerprint, ignoreWhitespace);
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out var node))
            {
                response = null!;
                return false;
            }

            _lru.Remove(node);
            _lru.AddLast(node);
            response = node.Value.Response;
            return true;
        }
    }

    public bool TryGetSibling(
        string repositoryPath,
        string path,
        string fingerprint,
        bool ignoreWhitespace,
        out ConflictResolutionResponse response) =>
        TryGet(repositoryPath, path, fingerprint, !ignoreWhitespace, out response);

    public void Set(
        string repositoryPath,
        string path,
        string fingerprint,
        bool ignoreWhitespace,
        ConflictResolutionResponse response)
    {
        var key = Key(repositoryPath, path, fingerprint, ignoreWhitespace);
        lock (_gate)
        {
            Remove(key);
            var node = _lru.AddLast(new Entry(key, response));
            _entries[key] = node;
            while (_entries.Count > capacity && _lru.First is { } oldest)
            {
                Remove(oldest.Value.Key);
            }
        }
    }

    public void RemoveStale(string repositoryPath, string path, string fingerprint)
    {
        lock (_gate)
        {
            var currentPrefix = Prefix(repositoryPath, path);
            var fingerprintPart = $"\0{fingerprint}\0";
            var stale = _entries.Keys
                .Where(key => key.StartsWith(currentPrefix, StringComparison.Ordinal)
                    && !key.Contains(fingerprintPart, StringComparison.Ordinal))
                .ToArray();
            foreach (var key in stale) Remove(key);
        }
    }

    public void Invalidate(string repositoryPath, string path)
    {
        lock (_gate)
        {
            var prefix = Prefix(repositoryPath, path);
            var matches = _entries.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .ToArray();
            foreach (var key in matches) Remove(key);
        }
    }

    private void Remove(string key)
    {
        if (!_entries.Remove(key, out var node)) return;
        _lru.Remove(node);
    }

    private static string Key(
        string repositoryPath,
        string path,
        string fingerprint,
        bool ignoreWhitespace) =>
        $"{Prefix(repositoryPath, path)}{fingerprint}\0{ignoreWhitespace}";

    private static string Prefix(string repositoryPath, string path) =>
        $"{NormalizeRepositoryPath(repositoryPath)}\0{path}\0";

    private static string NormalizeRepositoryPath(string path)
    {
        var normalized = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return OperatingSystem.IsWindows() ? normalized.ToUpperInvariant() : normalized;
    }

    private sealed record Entry(string Key, ConflictResolutionResponse Response);
}
