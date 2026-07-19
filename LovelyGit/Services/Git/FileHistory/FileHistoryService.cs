namespace ExpressThat.LovelyGit.Services.Git.FileHistory;

internal delegate Task<FileHistoryResponse> FileHistoryRead(
    string repositoryPath,
    string path,
    string? startCommitHash,
    int limit,
    int maximumCommits,
    TimeSpan maximumDuration,
    CancellationToken cancellationToken);

internal sealed class FileHistoryService : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _activeReads = new();
    private readonly FileHistoryRead _read;

    public FileHistoryService() : this(NativeFileHistoryReader.ReadAsync)
    {
    }

    internal FileHistoryService(FileHistoryRead read)
    {
        _read = read;
    }

    public void Cancel(Guid repositoryId)
    {
        CancellationTokenSource? activeRead;
        lock (_gate)
        {
            _activeReads.Remove(repositoryId, out activeRead);
        }

        activeRead?.Cancel();
    }

    public async Task<FileHistoryResponse> ReadAsync(
        Guid repositoryId,
        string repositoryPath,
        string path,
        string? startCommitHash,
        int limit,
        bool deep)
    {
        var source = new CancellationTokenSource();
        CancellationTokenSource? previous;
        lock (_gate)
        {
            _activeReads.Remove(repositoryId, out previous);
            _activeReads[repositoryId] = source;
        }

        previous?.Cancel();
        try
        {
            return await _read(
                repositoryPath,
                path,
                startCommitHash,
                limit,
                deep ? NativeFileHistoryReader.DeepMaximumCommits : NativeFileHistoryReader.DefaultMaximumCommits,
                deep ? NativeFileHistoryReader.DeepMaximumDuration : NativeFileHistoryReader.DefaultMaximumDuration,
                source.Token).ConfigureAwait(false);
        }
        finally
        {
            lock (_gate)
            {
                if (_activeReads.TryGetValue(repositoryId, out var current)
                    && ReferenceEquals(current, source))
                {
                    _activeReads.Remove(repositoryId);
                }
            }

            source.Dispose();
        }
    }

    public void Dispose()
    {
        List<CancellationTokenSource> reads;
        lock (_gate)
        {
            reads = _activeReads.Values.ToList();
            _activeReads.Clear();
        }

        foreach (var read in reads)
        {
            read.Cancel();
        }
    }
}
