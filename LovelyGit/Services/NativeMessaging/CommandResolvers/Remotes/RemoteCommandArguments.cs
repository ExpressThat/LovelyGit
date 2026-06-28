using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Remotes;

[TypeSharp]
public record GetRepositoryRemotesCommandArguments
{
    public Guid RepositoryId { get; set; }
}
