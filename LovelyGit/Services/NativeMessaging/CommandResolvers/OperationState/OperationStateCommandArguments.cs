using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.OperationState;

[TypeSharp]
public sealed record GetGitOperationStateCommandArguments
{
    public Guid RepositoryId { get; set; }
}
