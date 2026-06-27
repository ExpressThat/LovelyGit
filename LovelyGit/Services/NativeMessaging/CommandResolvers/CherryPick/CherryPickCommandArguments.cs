using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;

[TypeSharp]
public record CherryPickCommitCommandArguments
{
    public Guid RepositoryId { get; set; }

    public string CommitHash { get; set; } = string.Empty;
}
