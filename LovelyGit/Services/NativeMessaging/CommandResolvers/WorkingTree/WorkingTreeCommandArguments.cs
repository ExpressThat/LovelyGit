using ExpressThat.LovelyGit.Services.TypeGeneration;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[TypeSharp]
public record GetWorkingTreeChangesCommandArguments
{
    public Guid RepositoryId { get; set; }
    public bool AllowIncompleteSummary { get; set; }
    public bool TrackedOnly { get; set; }
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
    public bool Amend { get; set; }
    public bool Sign { get; set; }
}

[TypeSharp]
public record StageWorkingTreeHunkCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public List<WorkingTreePatchLine> Lines { get; set; } = new();
}

[TypeSharp]
public record IgnoreWorkingTreePathCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public GitIgnoreTarget Target { get; set; }
}

[TypeSharp]
public record GetHeadCommitMessageCommandArguments
{
    public Guid RepositoryId { get; set; }
}

[TypeSharp]
public record UndoLastCommitCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string ExpectedHeadHash { get; set; } = string.Empty;
}

[TypeSharp]
public record GitRemoteCommandArguments
{
    public Guid RepositoryId { get; set; }
    public bool Prune { get; set; }
    public GitPullMode PullMode { get; set; } = GitPullMode.Merge;
    public GitPushMode PushMode { get; set; } = GitPushMode.Normal;
    public string? RemoteName { get; set; }
}

[TypeSharp]
public record GetRemotesCommandArguments
{
    public Guid RepositoryId { get; set; }
}

[TypeSharp]
public record ManageRemoteCommandArguments
{
    public Guid RepositoryId { get; set; }
    public RemoteMutationAction Action { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NewName { get; set; }
    public string? Url { get; set; }
    public string? PushUrl { get; set; }
}

[TypeSharp]
public record CheckoutBranchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
}

[TypeSharp]
public record CheckoutCommitCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string CommitHash { get; set; } = string.Empty;
}

[TypeSharp]
public record CreateBranchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? StartPoint { get; set; }
    public bool Checkout { get; set; } = true;
}

[TypeSharp]
public record StashCommandArguments
{
    public Guid RepositoryId { get; set; }
    public StashAction Action { get; set; }
    public string? Selector { get; set; }
    public string? Message { get; set; }
    public bool IncludeUntracked { get; set; }
    public bool RestoreIndex { get; set; }
    public string? BranchName { get; set; }
    public bool SelectedOnly { get; set; }
    public List<string> Paths { get; set; } = new();
}
