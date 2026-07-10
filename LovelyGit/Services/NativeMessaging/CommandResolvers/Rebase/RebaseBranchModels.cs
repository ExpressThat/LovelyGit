using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

[TypeSharp]
public sealed record RebaseCurrentBranchOntoBranchCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BranchName { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record GetInteractiveRebasePlanCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BaseCommitHash { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record InteractiveRebasePlanResponse
{
    public string BaseCommitHash { get; init; } = string.Empty;
    public string CurrentBranchName { get; init; } = string.Empty;
    public List<InteractiveRebaseCommit> Commits { get; init; } = [];
}

[TypeSharp]
public sealed record InteractiveRebaseCommit
{
    public string Hash { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public long AuthorUnixSeconds { get; init; }
}

[TypeSharp]
public sealed record StartInteractiveRebaseCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BaseCommitHash { get; init; } = string.Empty;
    public List<InteractiveRebasePlanItem> Plan { get; init; } = [];
}

[TypeSharp]
public sealed record InteractiveRebasePlanItem
{
    public string Hash { get; init; } = string.Empty;
    public InteractiveRebaseAction Action { get; init; }
    public string? Message { get; init; }
}
