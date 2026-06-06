using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

[TranspilationSource]
public record GetCommitFileDiffCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string CommitHash { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CommitDiffViewMode ViewMode { get; set; } = CommitDiffViewMode.SideBySide;
}
