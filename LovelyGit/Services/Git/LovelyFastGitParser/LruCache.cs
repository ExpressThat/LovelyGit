namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed class LruCache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly long _maxWeight;
    private readonly Func<TValue, long> _weightSelector;
    private readonly object _gate = new();
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _map = new();
    private readonly LinkedList<Entry> _order = new();
    private long _currentWeight;

    public LruCache(int capacity)
        : this(capacity, Math.Max(1, capacity), _ => 1)
    {
    }

    public LruCache(int capacity, long maxWeight, Func<TValue, long> weightSelector)
    {
        _capacity = Math.Max(1, capacity);
        _maxWeight = Math.Max(1, maxWeight);
        _weightSelector = weightSelector;
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

    public long CurrentWeight
    {
        get
        {
            lock (_gate) return _currentWeight;
        }
    }

    public void Set(TKey key, TValue value)
    {
        lock (_gate)
        {
            var weight = Math.Max(1, _weightSelector(value));
            if (weight > _maxWeight)
            {
                Remove(key);
                return;
            }

            if (_map.TryGetValue(key, out var existing))
            {
                _currentWeight -= existing.Value.Weight;
                existing.Value = new Entry(key, value, weight);
                _currentWeight += weight;
                _order.Remove(existing);
                _order.AddFirst(existing);
                Trim();
                return;
            }

            var node = new LinkedListNode<Entry>(new Entry(key, value, weight));
            _map.Add(key, node);
            _order.AddFirst(node);
            _currentWeight += weight;

            Trim();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _map.Clear();
            _order.Clear();
            _currentWeight = 0;
        }
    }

    private void Trim()
    {
        while (
            (_map.Count > _capacity || _currentWeight > _maxWeight)
            && _order.Last is { } last)
        {
            Remove(last.Value.Key);
        }
    }

    private void Remove(TKey key)
    {
        if (!_map.Remove(key, out var node))
        {
            return;
        }

        _currentWeight -= node.Value.Weight;
        _order.Remove(node);
    }

    private readonly record struct Entry(TKey Key, TValue Value, long Weight);
}
