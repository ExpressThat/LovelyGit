using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

[TypeSharp]
public record CommitPatchSeriesCommandArguments
{
    public Guid RepositoryId { get; set; }
    public List<string> CommitHashes { get; set; } = [];
}
