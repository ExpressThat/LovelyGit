using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TypeSharp]
public sealed record RepositoryRefsResponse
{
    public string? CurrentBranchName { get; init; }
    public List<string> RemotePrefixes { get; init; } = new();
    public List<RepositoryRefItem> Refs { get; init; } = new();
    public List<RepositoryWorktreeItem> Worktrees { get; init; } = new();
    public List<RepositoryStashItem> Stashes { get; init; } = new();
}

[TypeSharp]
public sealed record RepositoryStashItem
{
    public string Selector { get; init; } = string.Empty;
    public string CommitHash { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public long? CreatedAtUnixSeconds { get; init; }
}

[TypeSharp]
public sealed record RepositoryRefItem
{
    public string Name { get; init; } = string.Empty;
    public string CommitHash { get; init; } = string.Empty;
    public CommitRefKind Kind { get; init; }
    public string? RemoteUrl { get; init; }
}

[TypeSharp]
public sealed record RepositoryWorktreeItem
{
    public string Path { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsLocked { get; init; }
    public string LockReason { get; init; } = string.Empty;
}
