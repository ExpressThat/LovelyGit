namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed class LruCache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly object _gate = new();
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _map = new();
    private readonly LinkedList<Entry> _order = new();

    public LruCache(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public bool TryGet(TKey key, out TValue value)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out var node))
            {
                _order.Remove(node);
                _order.AddFirst(node);
                value = node.Value.Value;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public void Set(TKey key, TValue value)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out var existing))
            {
                existing.Value = new Entry(key, value);
                _order.Remove(existing);
                _order.AddFirst(existing);
                return;
            }

            var node = new LinkedListNode<Entry>(new Entry(key, value));
            _map.Add(key, node);
            _order.AddFirst(node);

            while (_map.Count > _capacity && _order.Last is { } last)
            {
                _map.Remove(last.Value.Key);
                _order.RemoveLast();
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _map.Clear();
            _order.Clear();
        }
    }

    private readonly record struct Entry(TKey Key, TValue Value);
}
