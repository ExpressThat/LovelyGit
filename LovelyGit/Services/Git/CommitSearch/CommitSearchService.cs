using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.CommitSearch;

internal sealed class CommitSearchService : IDisposable
{
    private const int MaximumRetainedSessions = 3;
    private static readonly TimeSpan DefaultSessionRetention = TimeSpan.FromSeconds(30);
    private readonly object _gate = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _activeSearches = new();
    private readonly Dictionary<Guid, RetainedSearchSession> _retainedSessions = new();
    private readonly Timer _expirationTimer;
    private readonly TimeSpan _sessionRetention;
    private bool _disposed;

    public CommitSearchService() : this(DefaultSessionRetention)
    {
    }

    internal CommitSearchService(TimeSpan sessionRetention)
    {
        _sessionRetention = sessionRetention > TimeSpan.Zero
            ? sessionRetention
            : throw new ArgumentOutOfRangeException(nameof(sessionRetention));
        _expirationTimer = new Timer(
            static state => ((CommitSearchService)state!).ExpireSessions(),
            this,
            _sessionRetention,
            _sessionRetention);
    }

    internal int RetainedSessionCount
    {
        get { lock (_gate) return _retainedSessions.Count; }
    }

    public async Task<CommitSearchResponse> SearchAsync(
        Guid repositoryId,
        string repositoryPath,
        string query,
        string author,
        string scope,
        long? afterUnixSeconds,
        long? beforeUnixSeconds,
        int limit,
        bool deep)
    {
        var source = new CancellationTokenSource();
        CancellationTokenSource? previous;
        NativeCommitSearchSession? session = null;
        NativeCommitSearchSession? discardedSession = null;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _activeSearches.Remove(repositoryId, out previous);
            _activeSearches[repositoryId] = source;
            if (_retainedSessions.Remove(repositoryId, out var retained))
            {
                if (deep && retained.Session.Matches(
                    repositoryPath,
                    query,
                    author,
                    scope,
                    afterUnixSeconds,
                    beforeUnixSeconds,
                    limit))
                {
                    session = retained.Session;
                }
                else
                {
                    discardedSession = retained.Session;
                }
            }
        }

        previous?.Cancel();
        discardedSession?.Dispose();
        try
        {
            session ??= await NativeCommitSearchSession.OpenAsync(
                repositoryPath, query, author, scope, afterUnixSeconds, beforeUnixSeconds, limit, source.Token)
                .ConfigureAwait(false);
            var response = await session.ScanAsync(
                deep
                    ? NativeCommitSearchReader.DeepMaximumCommits
                    : NativeCommitSearchReader.DefaultMaximumCommits,
                deep
                    ? NativeCommitSearchReader.DeepMaximumDuration
                    : NativeCommitSearchReader.DefaultMaximumDuration,
                source.Token,
                deep ? null : NativeCommitSearchReader.DefaultResponsiveMatchScanCount)
                .ConfigureAwait(false);
            if (response.IsPartial && TryRetain(repositoryId, source, session))
            {
                session = null;
            }
            return response;
        }
        finally
        {
            session?.Dispose();
            lock (_gate)
            {
                if (_activeSearches.TryGetValue(repositoryId, out var current)
                    && ReferenceEquals(current, source))
                {
                    _activeSearches.Remove(repositoryId);
                }
            }

            source.Dispose();
        }
    }

    public void Dispose()
    {
        List<CancellationTokenSource> searches;
        List<NativeCommitSearchSession> sessions;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            searches = _activeSearches.Values.ToList();
            _activeSearches.Clear();
            sessions = _retainedSessions.Values.Select(value => value.Session).ToList();
            _retainedSessions.Clear();
        }

        _expirationTimer.Dispose();
        foreach (var search in searches)
        {
            search.Cancel();
        }
        foreach (var session in sessions)
        {
            session.Dispose();
        }
    }

    private bool TryRetain(
        Guid repositoryId,
        CancellationTokenSource source,
        NativeCommitSearchSession session)
    {
        NativeCommitSearchSession? evicted = null;
        lock (_gate)
        {
            if (_disposed
                || !_activeSearches.TryGetValue(repositoryId, out var current)
                || !ReferenceEquals(current, source))
            {
                return false;
            }

            if (_retainedSessions.Count >= MaximumRetainedSessions)
            {
                var oldest = _retainedSessions.MinBy(pair => pair.Value.RetainedAt);
                _retainedSessions.Remove(oldest.Key);
                evicted = oldest.Value.Session;
            }
            _retainedSessions[repositoryId] = new(session, Stopwatch.GetTimestamp());
        }
        evicted?.Dispose();
        return true;
    }

    private void ExpireSessions()
    {
        List<NativeCommitSearchSession> expired = [];
        lock (_gate)
        {
            if (_disposed) return;
            foreach (var (repositoryId, retained) in _retainedSessions.ToArray())
            {
                if (Stopwatch.GetElapsedTime(retained.RetainedAt) < _sessionRetention) continue;
                _retainedSessions.Remove(repositoryId);
                expired.Add(retained.Session);
            }
        }
        foreach (var session in expired) session.Dispose();
    }

    private sealed record RetainedSearchSession(
        NativeCommitSearchSession Session,
        long RetainedAt);
}
