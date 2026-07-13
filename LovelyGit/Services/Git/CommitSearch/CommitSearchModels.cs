using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitSearch;

[TypeSharp]
public sealed record CommitSearchResponse
{
    public string Query { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public long? AfterUnixSeconds { get; init; }
    public long? BeforeUnixSeconds { get; init; }
    public List<CommitSearchResult> Results { get; init; } = new();
    public int ScannedCommitCount { get; init; }
    public int MatchingCommitCount { get; init; }
    public bool IsPartial { get; init; }
}

[TypeSharp]
public sealed record CommitSearchResult
{
    public string Hash { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public long Date { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Preview { get; init; } = string.Empty;
    public List<string> Refs { get; init; } = new();
}
