using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

[TypeSharp]
public sealed record WorkingTreePatchLine
{
    public string ChangeType { get; set; } = string.Empty;

    public int? OldLineNumber { get; set; }

    public int? NewLineNumber { get; set; }

    public string OldText { get; set; } = string.Empty;

    public string NewText { get; set; } = string.Empty;
}
