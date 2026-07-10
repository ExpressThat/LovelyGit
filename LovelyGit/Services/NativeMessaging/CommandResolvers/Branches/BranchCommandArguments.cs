using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

[TypeSharp]
public sealed record RenameBranchCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string NewBranchName { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record DeleteBranchCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public bool Force { get; init; }
}

[TypeSharp]
public sealed record PushBranchCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string RemoteName { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record ManageBranchUpstreamCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string? UpstreamName { get; init; }
}

[TypeSharp]
public sealed record CreateBranchFromTagCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string TagName { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
}
