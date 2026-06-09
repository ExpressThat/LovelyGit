using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.WorkingTree;

[TranspilationSource]
public record GetWorkingTreeChangesCommandArguments
{
    public Guid RepositoryId { get; set; }
}
