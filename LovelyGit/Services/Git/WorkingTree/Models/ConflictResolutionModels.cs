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
    public string? TextGzipBase64 { get; set; }
    public string? TextEncoding { get; set; }
}

[TypeSharp]
public record ConflictSourceMetadata
{
    public string Label { get; set; } = string.Empty;
    public string? RefName { get; set; }
    public string? ObjectId { get; set; }
}

[TypeSharp]
public record ConflictHunk
{
    public int Id { get; set; }
    public int BaseStartLine { get; set; }
    public int BaseLineCount { get; set; }
    public int CurrentStartLine { get; set; }
    public int CurrentLineCount { get; set; }
    public int IncomingStartLine { get; set; }
    public int IncomingLineCount { get; set; }
}

[TypeSharp]
public record ConflictResolutionResponse
{
    public string Path { get; set; } = string.Empty;
    public string WorktreeFingerprint { get; set; } = string.Empty;
    public string? CompactTextSchema { get; set; }
    public string? CompactTextBundleGzipBase64 { get; set; }
    public ConflictFileVersion Base { get; set; } = new();
    public ConflictFileVersion Ours { get; set; } = new();
    public ConflictFileVersion Theirs { get; set; } = new();
    public ConflictFileVersion Result { get; set; } = new();
    public ConflictSourceMetadata CurrentSource { get; set; } = new();
    public ConflictSourceMetadata IncomingSource { get; set; } = new();
    public List<ConflictHunk> Hunks { get; set; } = new();
    public CommitFileDiffResponse? CurrentComparison { get; set; }
    public CommitFileDiffResponse? IncomingComparison { get; set; }
}
