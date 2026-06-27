using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

[TypeSharp]
public record RebaseCurrentBranchOntoBranchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
}
