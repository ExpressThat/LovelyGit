using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.FileHistory;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<FileHistoryChangeKind>))]
public enum FileHistoryChangeKind
{
    Added,
    Modified,
    Deleted,
    TypeChanged,
    Renamed,
}

[TypeSharp]
public sealed record FileHistoryResponse
{
    public string Path { get; init; } = string.Empty;
    public List<FileHistoryResult> Results { get; init; } = new();
    public int ScannedCommitCount { get; init; }
    public int MatchingCommitCount { get; init; }
    public bool IsPartial { get; init; }
}

[TypeSharp]
public sealed record FileHistoryResult
{
    public string Hash { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public long Date { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string? PreviousPath { get; init; }
    public FileHistoryChangeKind ChangeKind { get; init; }
}
