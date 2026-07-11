using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

[TypeSharp]
public record GetCommitDetailsCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string CommitHash { get; set; } = string.Empty;
    public int ParentIndex { get; set; }
}
