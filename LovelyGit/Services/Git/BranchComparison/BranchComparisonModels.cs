using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.BranchComparison;

[TypeSharp]
public sealed record GetBranchComparisonCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string TargetBranchName { get; init; } = string.Empty;
    public string? CurrentCommitHash { get; init; }
    public string? TargetCommitHash { get; init; }
}

[TypeSharp]
public sealed record BranchComparisonResponse
{
    public string CurrentBranchName { get; init; } = string.Empty;
    public string TargetBranchName { get; init; } = string.Empty;
    public string CurrentHash { get; init; } = string.Empty;
    public string TargetHash { get; init; } = string.Empty;
    public string? MergeBaseHash { get; init; }
    public int AheadCount { get; init; }
    public int BehindCount { get; init; }
    public int ChangedFileCount { get; init; }
    public bool IsHistoryPartial { get; init; }
    public bool IsFileListTruncated { get; init; }
    public string? CompactFilesGzipBase64 { get; init; }
    public List<BranchComparisonCommit> AheadCommits { get; init; } = [];
    public List<BranchComparisonCommit> BehindCommits { get; init; } = [];
    public List<BranchComparisonFile> Files { get; init; } = [];
}

[TypeSharp]
public sealed record BranchComparisonCommit
{
    public string Hash { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public long AuthorUnixSeconds { get; init; }
}

[TypeSharp]
public sealed record BranchComparisonFile
{
    public string Path { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
