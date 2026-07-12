namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed record GitRef(string Name, GitObjectId Target, GitRefKind Kind);

internal enum GitRefKind
{
    Head,
    Remote,
    Tag,
    Stash,
    Other,
}

internal sealed record GitCommitRef(string Name, GitRefKind Kind);

internal sealed record GitTag(GitObjectId Hash, GitObjectId Target, string Name, string TargetType);

internal enum GitSignatureKind
{
    None,
    OpenPgp,
    Ssh,
    X509,
    Unknown,
}

internal sealed class GitCommit
{
    private GitObjectId _firstParentHash;
    private List<GitObjectId>? _extraParentHashes;
    private List<GitObjectId>? _parentHashesSnapshot;
    private List<GitCommitRef>? _refs;

    public GitObjectId Hash { get; init; }
    public GitObjectId? TreeHash { get; set; }
    public int ParentHashCount { get; private set; }
    public IReadOnlyList<GitObjectId> ParentHashes => GetParentHashesSnapshot();
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public long AuthorUnixSeconds { get; set; }
    public long CommitterUnixSeconds { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public GitSignatureKind SignatureKind { get; set; }
    public IReadOnlyList<GitCommitRef> Refs => _refs ?? [];

    public void AddParentHash(GitObjectId id)
    {
        if (ParentHashCount == 0)
        {
            _firstParentHash = id;
        }
        else
        {
            (_extraParentHashes ??= new List<GitObjectId>(1)).Add(id);
        }

        _parentHashesSnapshot = null;
        ParentHashCount++;
    }

    public GitObjectId GetParentHash(int index)
    {
        if ((uint)index >= (uint)ParentHashCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return index == 0 ? _firstParentHash : _extraParentHashes![index - 1];
    }

    public void AddRefs(IEnumerable<GitCommitRef> refs)
    {
        _refs ??= new List<GitCommitRef>();
        _refs.AddRange(refs);
    }

    private IReadOnlyList<GitObjectId> GetParentHashesSnapshot()
    {
        if (ParentHashCount == 0)
        {
            return [];
        }

        if (_parentHashesSnapshot != null)
        {
            return _parentHashesSnapshot;
        }

        var snapshot = new List<GitObjectId>(ParentHashCount) { _firstParentHash };
        if (_extraParentHashes != null)
        {
            snapshot.AddRange(_extraParentHashes);
        }

        _parentHashesSnapshot = snapshot;
        return snapshot;
    }
}

internal readonly record struct GitCommitSearchHeader(
    GitObjectId FirstParentHash,
    GitObjectId[]? AdditionalParentHashes,
    int ParentHashCount,
    long AuthorUnixSeconds,
    bool IsMatch)
{
    public GitObjectId GetParentHash(int index)
    {
        if ((uint)index >= (uint)ParentHashCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return index == 0 ? FirstParentHash : AdditionalParentHashes![index - 1];
    }
}

internal readonly record struct GitCommitTraversalHeader(
    GitObjectId? TreeHash,
    GitObjectId FirstParentHash,
    GitObjectId[]? AdditionalParentHashes,
    int ParentHashCount,
    long AuthorUnixSeconds)
{
    public GitObjectId GetParentHash(int index)
    {
        if ((uint)index >= (uint)ParentHashCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return index == 0 ? FirstParentHash : AdditionalParentHashes![index - 1];
    }
}

internal readonly record struct GitTreePathEntry(GitObjectId ObjectId, string Mode)
{
    public bool IsTree => Mode == "40000";
}

internal sealed record GitTreeFile(string Path, GitObjectId ObjectId, string Mode);

internal sealed record GitTreeComparison(
    IReadOnlyDictionary<string, GitTreeFile> ParentFiles,
    IReadOnlyDictionary<string, GitTreeFile> CurrentFiles);

internal sealed record GitTreeEntry(string Name, string Path, GitObjectId ObjectId, string Mode)
{
    public bool IsTree => Mode == "40000";
}

internal enum GitObjectKind
{
    Commit,
    Tree,
    Blob,
    Tag,
}

internal sealed record GitObjectData(GitObjectKind Kind, byte[] Data);
