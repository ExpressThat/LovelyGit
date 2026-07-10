using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;

[TypeSharp]
public sealed record RepositoryOperationCommandResponse
{
    public bool IsCompleted { get; init; }
    public GitRepositoryOperationKind? Operation { get; init; }
    public string? Message { get; init; }
}

[TypeSharp]
public sealed record GetRepositoryOperationStateCommandArguments
{
    public Guid RepositoryId { get; init; }
}

[TypeSharp]
public sealed record RepositoryOperationCommandArguments
{
    public Guid RepositoryId { get; init; }
    public GitRepositoryOperationKind Operation { get; init; }
}

[TypeSharp]
public sealed record RepositoryOperationStateResponse
{
    public GitRepositoryOperationKind? Operation { get; init; }
}
