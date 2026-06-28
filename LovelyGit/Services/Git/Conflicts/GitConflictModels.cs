using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.OperationState;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.Conflicts;

[TypeSharp]
public sealed record GitConflictStateResponse
{
    public GitOperationState Operation { get; init; } = new();
    public string OursLabel { get; init; } = "Current version";
    public string TheirsLabel { get; init; } = "Incoming version";
    public List<GitConflictFile> ConflictedFiles { get; init; } = new();
    public List<GitConflictFile> ResolvedFiles { get; init; } = new();
    public string CommitMessage { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record GitConflictFile
{
    public string Path { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ConflictCount { get; init; }
    public bool IsBinary { get; init; }
}

[TypeSharp]
public sealed record GitConflictFileContentResponse
{
    public string Path { get; init; } = string.Empty;
    public bool IsBinary { get; init; }
    public int ConflictCount { get; init; }
    public List<GitConflictTextLine> OursLines { get; init; } = new();
    public List<GitConflictTextLine> TheirsLines { get; init; } = new();
    public List<GitConflictTextLine> ResultLines { get; init; } = new();
}

[TypeSharp]
public sealed record GitConflictTextLine
{
    public int LineNumber { get; init; }
    public string Text { get; init; } = string.Empty;
    public string MarkerKind { get; init; } = string.Empty;
    public List<CommitFileDiffSyntaxSpan> SyntaxSpans { get; init; } = new();
}
