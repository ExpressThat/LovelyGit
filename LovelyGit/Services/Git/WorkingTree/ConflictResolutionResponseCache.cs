using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class ConflictResolutionResponseCache(
    int capacity = 8,
    long maximumCharacterWeight = 8 * 1024 * 1024)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedListNode<Entry>> _entries = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> _lru = new();
    private long _currentCharacterWeight;

    internal int Count
    {
        get
        {
            lock (_gate) return _entries.Count;
        }
    }

    internal long CurrentCharacterWeight
    {
        get
        {
            lock (_gate) return _currentCharacterWeight;
        }
    }

    public bool TryGet(
        string repositoryPath,
        string path,
        string fingerprint,
        bool ignoreWhitespace,
        out ConflictResolutionResponse response) =>
        TryGetEntry(
            repositoryPath,
            path,
            fingerprint,
            ignoreWhitespace,
            out response,
            out _);

    private bool TryGetEntry(
        string repositoryPath,
        string path,
        string fingerprint,
        bool ignoreWhitespace,
        out ConflictResolutionResponse response,
        out ConflictTexts? retainedTexts)
    {
        var key = Key(repositoryPath, path, fingerprint, ignoreWhitespace);
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out var node))
            {
                response = null!;
                retainedTexts = null;
                return false;
            }

            _lru.Remove(node);
            _lru.AddLast(node);
            response = node.Value.Response;
            retainedTexts = node.Value.RetainedTexts;
            return true;
        }
    }

    public bool TryGetSibling(
        string repositoryPath,
        string path,
        string fingerprint,
        bool ignoreWhitespace,
        out ConflictResolutionResponse response,
        out ConflictTexts? retainedTexts) =>
        TryGetEntry(
            repositoryPath,
            path,
            fingerprint,
            !ignoreWhitespace,
            out response,
            out retainedTexts);

    public bool TryGetCurrent(
        string repositoryPath,
        string path,
        bool ignoreWhitespace,
        out ConflictResolutionResponse response,
        out ConflictResolutionCacheStamp stamp) =>
        TryGetCurrent(
            repositoryPath,
            path,
            ignoreWhitespace,
            out response,
            out stamp,
            out _);

    private bool TryGetCurrent(
        string repositoryPath,
        string path,
        bool ignoreWhitespace,
        out ConflictResolutionResponse response,
        out ConflictResolutionCacheStamp stamp,
        out ConflictTexts? retainedTexts)
    {
        var normalizedRepository = NormalizeRepositoryPath(repositoryPath);
        lock (_gate)
        {
            for (var node = _lru.Last; node is not null; node = node.Previous)
            {
                var entry = node.Value;
                if (entry.RepositoryPath != normalizedRepository || entry.Path != path ||
                    entry.IgnoreWhitespace != ignoreWhitespace || entry.Stamp is not { } candidate)
                {
                    continue;
                }

                if (!candidate.IsCurrent())
                {
                    RemoveOwner(normalizedRepository, path);
                    break;
                }

                _lru.Remove(node);
                _lru.AddLast(node);
                response = entry.Response;
                stamp = candidate;
                retainedTexts = entry.RetainedTexts;
                return true;
            }
        }

        response = null!;
        stamp = default;
        retainedTexts = null;
        return false;
    }

    public bool TryGetCurrentSibling(
        string repositoryPath,
        string path,
        bool ignoreWhitespace,
        out ConflictResolutionResponse response,
        out ConflictResolutionCacheStamp stamp,
        out ConflictTexts? retainedTexts) =>
        TryGetCurrent(
            repositoryPath,
            path,
            !ignoreWhitespace,
            out response,
            out stamp,
            out retainedTexts);

    public void Set(
        string repositoryPath,
        string path,
        string fingerprint,
        bool ignoreWhitespace,
        ConflictResolutionResponse response,
        ConflictResolutionCacheStamp? stamp = null,
        ConflictTexts? retainedTexts = null)
    {
        var key = Key(repositoryPath, path, fingerprint, ignoreWhitespace);
        var normalizedRepository = NormalizeRepositoryPath(repositoryPath);
        var characterWeight = ConflictResolutionResponseWeight.Estimate(response, retainedTexts);
        lock (_gate)
        {
            Remove(key);
            var node = _lru.AddLast(new Entry(
                key,
                normalizedRepository,
                path,
                ignoreWhitespace,
                stamp,
                retainedTexts,
                characterWeight,
                response));
            _entries[key] = node;
            _currentCharacterWeight += characterWeight;
            while ((_entries.Count > capacity || _currentCharacterWeight > maximumCharacterWeight)
                && _lru.First is { } oldest)
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
        _currentCharacterWeight -= node.Value.CharacterWeight;
        _lru.Remove(node);
    }

    private void RemoveOwner(string repositoryPath, string path)
    {
        var matches = _lru
            .Where(entry => entry.RepositoryPath == repositoryPath && entry.Path == path)
            .Select(entry => entry.Key)
            .ToArray();
        foreach (var key in matches) Remove(key);
    }

    private sealed record Entry(
        string Key,
        string RepositoryPath,
        string Path,
        bool IgnoreWhitespace,
        ConflictResolutionCacheStamp? Stamp,
        ConflictTexts? RetainedTexts,
        long CharacterWeight,
        ConflictResolutionResponse Response);
}
