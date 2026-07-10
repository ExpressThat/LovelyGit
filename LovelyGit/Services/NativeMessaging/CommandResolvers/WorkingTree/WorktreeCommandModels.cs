using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[TypeSharp]
public sealed record CreateWorktreeCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string WorktreePath { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record ManageWorktreeCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string WorktreePath { get; init; } = string.Empty;
    public WorktreeMutationAction Action { get; init; }
    public bool Force { get; init; }
    public string? LockReason { get; init; }
}

[TypeSharp]
public sealed record WorktreeDestinationResponse
{
    public string Path { get; init; } = string.Empty;
}
