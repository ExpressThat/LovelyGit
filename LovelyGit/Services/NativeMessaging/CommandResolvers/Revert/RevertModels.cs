using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Revert;

[TypeSharp]
public sealed record RevertCommitCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string CommitHash { get; init; } = string.Empty;
}
