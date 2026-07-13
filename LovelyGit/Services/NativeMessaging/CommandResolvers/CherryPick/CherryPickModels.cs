using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;

[TypeSharp]
public sealed record CherryPickCommitCommandArguments
{
    public Guid RepositoryId { get; init; }
    public List<string> CommitHashes { get; init; } = new();
}
