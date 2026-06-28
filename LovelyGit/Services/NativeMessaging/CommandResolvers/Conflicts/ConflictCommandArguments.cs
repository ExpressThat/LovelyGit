using ExpressThat.LovelyGit.Services.Git.Conflicts;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Conflicts;

[TypeSharp]
public sealed record GetConflictStateCommandArguments
{
    public Guid RepositoryId { get; set; }
}

[TypeSharp]
public sealed record GetConflictFileContentCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
}

[TypeSharp]
public sealed record ResolveConflictFileCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public GitConflictAction Action { get; set; }
}

[TypeSharp]
public sealed record CompleteConflictOperationCommandArguments
{
    public Guid RepositoryId { get; set; }
}
