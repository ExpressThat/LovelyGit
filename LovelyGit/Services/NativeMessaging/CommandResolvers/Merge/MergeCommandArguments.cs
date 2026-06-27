using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;

[TypeSharp]
public record MergeBranchIntoCurrentCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
}
