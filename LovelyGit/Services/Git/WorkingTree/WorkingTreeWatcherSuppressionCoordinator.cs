namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class WorkingTreeWatcherSuppressionCoordinator
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, int> _counts = new();
    private Action<Guid, bool>? _changed;

    internal IDisposable Suppress(Guid repositoryId)
    {
        var notify = false;
        lock (_lock)
        {
            _counts.TryGetValue(repositoryId, out var count);
            _counts[repositoryId] = count + 1;
            notify = count == 0;
        }
        if (notify) Notify(repositoryId, suppressed: true);
        return new Suppression(this, repositoryId);
    }

    internal bool IsSuppressed(Guid repositoryId)
    {
        lock (_lock) return _counts.ContainsKey(repositoryId);
    }

    internal IDisposable Subscribe(Action<Guid, bool> observer)
    {
        lock (_lock) _changed += observer;
        return new Subscription(this, observer);
    }

    private void Release(Guid repositoryId)
    {
        var notify = false;
        lock (_lock)
        {
            if (!_counts.TryGetValue(repositoryId, out var count)) return;
            if (count > 1) _counts[repositoryId] = count - 1;
            else
            {
                _counts.Remove(repositoryId);
                notify = true;
            }
        }
        if (notify) Notify(repositoryId, suppressed: false);
    }

    private void Notify(Guid repositoryId, bool suppressed)
    {
        Action<Guid, bool>? changed;
        lock (_lock) changed = _changed;
        changed?.Invoke(repositoryId, suppressed);
    }

    private void Unsubscribe(Action<Guid, bool> observer)
    {
        lock (_lock) _changed -= observer;
    }

    private sealed class Suppression(
        WorkingTreeWatcherSuppressionCoordinator owner,
        Guid repositoryId) : IDisposable
    {
        private WorkingTreeWatcherSuppressionCoordinator? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Release(repositoryId);
    }

    private sealed class Subscription(
        WorkingTreeWatcherSuppressionCoordinator owner,
        Action<Guid, bool> observer) : IDisposable
    {
        private WorkingTreeWatcherSuppressionCoordinator? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Unsubscribe(observer);
    }
}
