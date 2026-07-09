using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;

[TypeSharp]
public sealed record MergeBranchIntoCurrentCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string BranchName { get; init; } = string.Empty;
}
