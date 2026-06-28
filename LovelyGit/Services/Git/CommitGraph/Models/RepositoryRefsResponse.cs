using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TypeSharp]
public sealed record RepositoryRefsResponse
{
    public string? CurrentBranchName { get; init; }
    public List<string> RemotePrefixes { get; init; } = new();
    public List<RepositoryRefItem> Refs { get; init; } = new();
}

[TypeSharp]
public sealed record RepositoryRefItem
{
    public string Name { get; init; } = string.Empty;
    public string CommitHash { get; init; } = string.Empty;
    public CommitRefKind Kind { get; init; }
    public string? RemoteUrl { get; init; }
}
