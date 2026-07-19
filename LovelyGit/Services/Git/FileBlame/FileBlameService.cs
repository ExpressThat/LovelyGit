namespace ExpressThat.LovelyGit.Services.Git.FileBlame;

internal delegate Task<FileBlameResponse> FileBlameRead(
    string repositoryPath,
    string path,
    string? startCommitHash,
    int maximumCommits,
    TimeSpan maximumDuration,
    CancellationToken cancellationToken);

internal sealed class FileBlameService : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _activeReads = new();
    private readonly FileBlameRead _read;

    public FileBlameService() : this(NativeFileBlameReader.ReadAsync)
    {
    }

    internal FileBlameService(FileBlameRead read)
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

    public async Task<FileBlameResponse> ReadAsync(
        Guid repositoryId,
        string repositoryPath,
        string path,
        string? startCommitHash,
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
                deep ? NativeFileBlameReader.DeepMaximumCommits : NativeFileBlameReader.DefaultMaximumCommits,
                deep ? NativeFileBlameReader.DeepMaximumDuration : NativeFileBlameReader.DefaultMaximumDuration,
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
