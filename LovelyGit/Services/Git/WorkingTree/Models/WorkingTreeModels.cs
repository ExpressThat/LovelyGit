using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using Tapper;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

[TranspilationSource]
[JsonConverter(typeof(JsonStringEnumConverter<WorkingTreeChangeGroup>))]
public enum WorkingTreeChangeGroup
{
    Staged,
    Unstaged,
    Untracked,
    Unmerged,
}

[TranspilationSource]
public record WorkingTreeChangedFile
{
    public string Path { get; set; } = string.Empty;
    public string? OldPath { get; set; }
    public string Status { get; set; } = string.Empty;
    public WorkingTreeChangeGroup Group { get; set; }
    public uint Additions { get; set; }
    public uint Deletions { get; set; }
    public bool IsBinary { get; set; }
}

[TranspilationSource]
public record WorkingTreeChangesResponse
{
    public List<WorkingTreeChangedFile> Staged { get; set; } = new();
    public List<WorkingTreeChangedFile> Unstaged { get; set; } = new();
    public List<WorkingTreeChangedFile> Untracked { get; set; } = new();
    public List<WorkingTreeChangedFile> Unmerged { get; set; } = new();

    public int TotalCount => Staged.Count + Unstaged.Count + Untracked.Count + Unmerged.Count;
}

[TranspilationSource]
public record WorkingTreeChangedNotification
{
    public int Generation { get; set; }
}

[TranspilationSource]
public record CommitGraphChangedNotification
{
    public int Generation { get; set; }
}

[TranspilationSource]
public record GetWorkingTreeFileDiffArguments
{
    public Guid RepositoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public WorkingTreeChangeGroup Group { get; set; }
    public CommitDiffViewMode ViewMode { get; set; } = CommitDiffViewMode.SideBySide;
}
