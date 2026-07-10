using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<WorkingTreeChangeGroup>))]
public enum WorkingTreeChangeGroup
{
    Staged,
    Unstaged,
    Untracked,
    Unmerged,
}

[TypeSharp]
public record WorkingTreeChangedFile
{
    public string Path { get; set; } = string.Empty;
    public string? OldPath { get; set; }
    public string Status { get; set; } = string.Empty;
    public WorkingTreeChangeGroup Group { get; set; }
    [TypeAs("number")]
    public uint Additions { get; set; }
    [TypeAs("number")]
    public uint Deletions { get; set; }
    public bool IsBinary { get; set; }
}

[TypeSharp]
public record WorkingTreeChangesResponse
{
    public List<WorkingTreeChangedFile> Staged { get; set; } = new();
    public List<WorkingTreeChangedFile> Unstaged { get; set; } = new();
    public List<WorkingTreeChangedFile> Untracked { get; set; } = new();
    public List<WorkingTreeChangedFile> Unmerged { get; set; } = new();

    public int TotalCount => Staged.Count + Unstaged.Count + Untracked.Count + Unmerged.Count;
}

[TypeSharp]
public record WorkingTreeChangeSummaryResponse
{
    public int TotalCount { get; set; }
    public bool IsComplete { get; set; } = true;
    public bool HasChanges => TotalCount > 0;
}

[TypeSharp]
public record WorkingTreeChangedNotification
{
    public int Generation { get; set; }
    public List<WorkingTreeChangedFile> ObservedChanges { get; set; } = new();
}

[TypeSharp]
public record CommitGraphChangedNotification
{
    public int Generation { get; set; }
}

[TypeSharp]
public record HeadCommitMessageResponse
{
    public string Hash { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

[TypeSharp]
public record GetWorkingTreeFileDiffArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public WorkingTreeChangeGroup Group { get; set; }
    public CommitDiffViewMode ViewMode { get; set; } = CommitDiffViewMode.SideBySide;
    public bool IgnoreWhitespace { get; set; }
}
