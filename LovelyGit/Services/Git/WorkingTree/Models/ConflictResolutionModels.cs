using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

[TypeSharp]
public record ConflictFileVersion
{
    public bool Exists { get; set; }
    public bool IsBinary { get; set; }
    public bool IsTooLarge { get; set; }
    public long SizeBytes { get; set; }
    public string? Text { get; set; }
}

[TypeSharp]
public record ConflictResolutionResponse
{
    public string Path { get; set; } = string.Empty;
    public string WorktreeFingerprint { get; set; } = string.Empty;
    public ConflictFileVersion Base { get; set; } = new();
    public ConflictFileVersion Ours { get; set; } = new();
    public ConflictFileVersion Theirs { get; set; } = new();
    public ConflictFileVersion Result { get; set; } = new();
    public CommitFileDiffResponse? Comparison { get; set; }
}
