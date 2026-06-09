namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed record GitRef(string Name, GitObjectId Target, GitRefKind Kind);

internal enum GitRefKind
{
    Head,
    Remote,
    Tag,
    Other,
}

internal sealed record GitCommitRef(string Name, GitRefKind Kind);

internal sealed record GitTag(GitObjectId Hash, GitObjectId Target, string Name, string TargetType);

internal sealed class GitCommit
{
    public GitObjectId Hash { get; init; }
    public GitObjectId? TreeHash { get; set; }
    public List<GitObjectId> ParentHashes { get; } = new();
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public long AuthorUnixSeconds { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> Branches { get; } = new();
    public List<string> Tags { get; } = new();
    public List<GitCommitRef> Refs { get; } = new();
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
