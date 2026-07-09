using ExpressThat.LovelyGit.Services.TypeGeneration;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[TypeSharp]
public record GetWorkingTreeChangesCommandArguments
{
    public Guid RepositoryId { get; set; }
    public bool AllowIncompleteSummary { get; set; }
}

[TypeSharp]
public record RevealWorkingTreeFileCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
}

[TypeSharp]
public record UpdateWorkingTreeIndexCommandArguments
{
    public Guid RepositoryId { get; set; }
    public bool IncludeAll { get; set; }
    public List<string> Paths { get; set; } = new();
}

[TypeSharp]
public record DiscardWorkingTreeChangesCommandArguments
{
    public Guid RepositoryId { get; set; }
    public List<WorkingTreeChangedFile> Files { get; set; } = new();
}

[TypeSharp]
public record StageWorkingTreeLineCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public int? OldLineNumber { get; set; }
    public int? NewLineNumber { get; set; }
    public string OldText { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
}

[TypeSharp]
public record CommitStagedChangesCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

[TypeSharp]
public record GitRemoteCommandArguments
{
    public Guid RepositoryId { get; set; }
    public GitPullMode PullMode { get; set; } = GitPullMode.Merge;
    public string? RemoteName { get; set; }
}

[TypeSharp]
public record CheckoutBranchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? LocalBranchName { get; set; }
    public bool IsRemote { get; set; }
}

[TypeSharp]
public record CreateBranchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string StartPoint { get; set; } = "HEAD";
    public bool Checkout { get; set; }
}

[TypeSharp]
public record DeleteBranchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool Force { get; set; }
}

[TypeSharp]
public record RenameBranchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string NewBranchName { get; set; } = string.Empty;
}
