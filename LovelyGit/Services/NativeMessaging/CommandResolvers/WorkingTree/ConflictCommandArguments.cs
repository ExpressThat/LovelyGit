using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[TypeSharp]
public record GetConflictResolutionCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public CommitDiffViewMode ViewMode { get; set; } = CommitDiffViewMode.SideBySide;
    public bool IgnoreWhitespace { get; set; }
}

[TypeSharp]
public record ResolveConflictCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string ExpectedFingerprint { get; set; } = string.Empty;
    public string? ResultText { get; set; }
    public string ResultTextGzipBase64 { get; set; } = string.Empty;
    public ConflictResolutionSource? Source { get; set; }
    public bool DeleteResult { get; set; }
}

[TypeSharp]
public record OpenConflictInMergeToolCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
}
