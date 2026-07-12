namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class GitIgnoreMatcherCache
{
    private const int DefaultCapacity = 4;
    private readonly int _capacity;
    private readonly Lock _lock = new();
    private readonly Dictionary<CacheKey, CacheEntry> _entries = new();
    private readonly LinkedList<CacheKey> _recency = new();

    public GitIgnoreMatcherCache(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
    }

    public bool TryGet(string workTreeDirectory, string gitDirectory, out GitIgnoreMatcher? matcher)
    {
        var key = new CacheKey(workTreeDirectory, gitDirectory);
        lock (_lock)
        {
            if (_entries.TryGetValue(key, out var entry) && entry.Matcher.SourcesAreCurrent())
            {
                _recency.Remove(entry.Node);
                _recency.AddLast(entry.Node);
                matcher = entry.Matcher;
                return true;
            }

            Remove(key);
            matcher = null;
            return false;
        }
    }

    public void Set(string workTreeDirectory, string gitDirectory, GitIgnoreMatcher matcher)
    {
        var key = new CacheKey(workTreeDirectory, gitDirectory);
        lock (_lock)
        {
            Remove(key);
            if (_entries.Count >= _capacity)
            {
                Remove(_recency.First!.Value);
            }

            var node = _recency.AddLast(key);
            _entries.Add(key, new CacheEntry(matcher, node));
        }
    }

    private void Remove(CacheKey key)
    {
        if (_entries.Remove(key, out var entry))
        {
            _recency.Remove(entry.Node);
        }
    }

    private sealed record CacheEntry(GitIgnoreMatcher Matcher, LinkedListNode<CacheKey> Node);
    private readonly record struct CacheKey(string WorkTreeDirectory, string GitDirectory);
}
