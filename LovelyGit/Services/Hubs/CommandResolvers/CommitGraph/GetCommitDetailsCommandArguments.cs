using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

[TranspilationSource]
public record GetCommitDetailsCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string CommitHash { get; set; } = string.Empty;
}
