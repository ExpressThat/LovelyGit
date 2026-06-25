using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<CommitDiffViewMode>))]
public enum CommitDiffViewMode
{
    SideBySide,
    Combined,
}

[TypeSharp]
public record CommitFileDiffResponse
{
    public string CommitHash { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public CommitDiffViewMode ViewMode { get; set; }
    public bool IsBinary { get; set; }
    public bool HasDifferences { get; set; }
    public List<CommitFileDiffLine> Lines { get; set; } = new();
}

[TypeSharp]
public record CommitFileDiffLine
{
    public int? OldLineNumber { get; set; }
    public int? NewLineNumber { get; set; }
    public string OldText { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public List<CommitFileDiffSyntaxSpan> OldSyntaxSpans { get; set; } = new();
    public List<CommitFileDiffSyntaxSpan> NewSyntaxSpans { get; set; } = new();
    public List<CommitFileDiffSyntaxSpan> SyntaxSpans { get; set; } = new();
    public List<CommitFileDiffChangeSpan> OldChangeSpans { get; set; } = new();
    public List<CommitFileDiffChangeSpan> NewChangeSpans { get; set; } = new();
    public List<CommitFileDiffChangeSpan> ChangeSpans { get; set; } = new();
}

[TypeSharp]
public record CommitFileDiffSyntaxSpan
{
    public int Start { get; set; }
    public int Length { get; set; }
    public string Scope { get; set; } = string.Empty;
}

[TypeSharp]
public record CommitFileDiffChangeSpan
{
    public int Start { get; set; }
    public int Length { get; set; }
    public string ChangeType { get; set; } = string.Empty;
}
