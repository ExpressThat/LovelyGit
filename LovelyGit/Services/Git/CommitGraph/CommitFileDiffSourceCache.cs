namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitFileDiffSourceCache
{
    private const int MaximumCharacters = 2_000_000;
    private readonly object _gate = new();
    private string? _key;
    private CommitFileDiffService.CommitFileDiffSource? _source;
    private string? _bundleKey;
    private string? _compressedSourceBundle;

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
        var retain = source.OldText.Length + source.NewText.Length <= MaximumCharacters;
        lock (_gate)
        {
            if (!string.Equals(_bundleKey, key, StringComparison.Ordinal))
            {
                _bundleKey = null;
                _compressedSourceBundle = null;
            }
            _key = retain ? key : null;
            _source = retain ? source : null;
        }
    }

    public bool TryGetCompressedSourceBundle(string key, out string bundle)
    {
        lock (_gate)
        {
            bundle = _compressedSourceBundle!;
            return bundle != null && string.Equals(_bundleKey, key, StringComparison.Ordinal);
        }
    }

    public void SetCompressedSourceBundle(string key, string bundle)
    {
        if (bundle.Length > MaximumCharacters) return;
        lock (_gate)
        {
            _bundleKey = key;
            _compressedSourceBundle = bundle;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _key = null;
            _source = null;
            _bundleKey = null;
            _compressedSourceBundle = null;
        }
    }
}
