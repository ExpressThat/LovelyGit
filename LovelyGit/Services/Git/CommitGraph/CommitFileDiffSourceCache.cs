namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitFileDiffSourceCache
{
    private const int MaximumCharacters = 2_000_000;
    private readonly object _gate = new();
    private string? _key;
    private CommitFileDiffService.CommitFileDiffSource? _source;

    public bool TryGet(string key, out CommitFileDiffService.CommitFileDiffSource source)
    {
        lock (_gate)
        {
            source = _source!;
            return _source != null && string.Equals(_key, key, StringComparison.Ordinal);
        }
    }

    public void Set(string key, CommitFileDiffService.CommitFileDiffSource source)
    {
        if (source.OldText.Length + source.NewText.Length > MaximumCharacters) return;
        lock (_gate)
        {
            _key = key;
            _source = source;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _key = null;
            _source = null;
        }
    }
}
