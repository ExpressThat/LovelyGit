using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

[TypeSharp]
public record GetCommitFileDiffCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string CommitHash { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CommitDiffViewMode ViewMode { get; set; } = CommitDiffViewMode.SideBySide;
}
