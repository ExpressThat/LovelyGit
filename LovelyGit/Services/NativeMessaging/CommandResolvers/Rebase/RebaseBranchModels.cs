using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

[TypeSharp]
public sealed record RebaseCurrentBranchOntoBranchCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BranchName { get; init; } = string.Empty;
}
