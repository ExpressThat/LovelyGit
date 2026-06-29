using System.Text.Json.Serialization;

namespace LovelyGit.DiffBenchmarks;

[JsonConverter(typeof(JsonStringEnumConverter<CommitDiffViewMode>))]
internal enum CommitDiffViewMode
{
    SideBySide,
    Combined,
}

[JsonConverter(typeof(CommitFileDiffResponseJsonConverter))]
internal sealed class CommitFileDiffResponse
{
    public string CommitHash { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public CommitDiffViewMode ViewMode { get; set; }
    public bool IsBinary { get; set; }
    public bool HasDifferences { get; set; }
    public bool IsTruncated { get; set; }
    public string TruncationMessage { get; set; } = string.Empty;
    public List<CommitFileDiffLine> Lines { get; set; } = new();
    [JsonIgnore]
    public DiffSerializationPlan? Plan { get; set; }
    [JsonIgnore]
    public int? PlannedRows { get; set; }
    [JsonIgnore]
    public Func<string>? JsonFactory { get; set; }
    [JsonIgnore]
    public Func<long>? PayloadByteCountFactory { get; set; }
}

internal sealed class CommitFileDiffLine
{
    public int? OldLineNumber { get; set; }
    public int? NewLineNumber { get; set; }
    public string? OldText { get; set; }
    public string? NewText { get; set; }
    public string? Text { get; set; }
    public string ChangeType { get; set; } = string.Empty;
}

internal sealed record BenchmarkCase(
    string Name,
    int LineCount,
    string OldText,
    string NewText,
    string Notes);

internal sealed record DiffSerializationPlan(
    string OldText,
    string NewText);

internal sealed record BenchmarkCandidate(
    string Name,
    string Category,
    int MaxLineCount,
    string Notes,
    Func<BenchmarkCase, CommitDiffViewMode, bool, CommitFileDiffResponse> Run);

internal sealed record BenchmarkResult(
    string Candidate,
    string Category,
    string CaseName,
    int LineCount,
    string ViewMode,
    string IgnoreWhitespace,
    string Status,
    double DiffMs,
    double SerializeMs,
    long PayloadBytes,
    long MemoryBytes,
    int Rows,
    string Notes);
