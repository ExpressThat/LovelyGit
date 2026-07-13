namespace ExpressThat.LovelyGit.Services.Git.CommitSearch;

internal sealed class CommitSearchService : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _activeSearches = new();

    public async Task<CommitSearchResponse> SearchAsync(
        Guid repositoryId,
        string repositoryPath,
        string query,
        string author,
        long? afterUnixSeconds,
        long? beforeUnixSeconds,
        int limit,
        bool deep)
    {
        var source = new CancellationTokenSource();
        CancellationTokenSource? previous;
        lock (_gate)
        {
            _activeSearches.Remove(repositoryId, out previous);
            _activeSearches[repositoryId] = source;
        }

        previous?.Cancel();
        try
        {
            return await NativeCommitSearchReader.SearchAsync(
                repositoryPath,
                query,
                author,
                afterUnixSeconds,
                beforeUnixSeconds,
                limit,
                deep
                    ? NativeCommitSearchReader.DeepMaximumCommits
                    : NativeCommitSearchReader.DefaultMaximumCommits,
                deep
                    ? NativeCommitSearchReader.DeepMaximumDuration
                    : NativeCommitSearchReader.DefaultMaximumDuration,
                source.Token).ConfigureAwait(false);
        }
        finally
        {
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
        lock (_gate)
        {
            searches = _activeSearches.Values.ToList();
            _activeSearches.Clear();
        }

        foreach (var search in searches)
        {
            search.Cancel();
        }
    }
}
