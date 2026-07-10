using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.FileBlame;

[TypeSharp]
public sealed record FileBlameResponse
{
    public string Path { get; init; } = string.Empty;
    public string StartCommitHash { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public List<FileBlameHunk> Hunks { get; init; } = new();
    public int LineCount { get; init; }
    public int ScannedCommitCount { get; init; }
    public int ResolvedLineCount { get; init; }
    public bool IsPartial { get; init; }
}

[TypeSharp]
public sealed record FileBlameHunk
{
    public int StartLine { get; init; }
    public int LineCount { get; init; }
    public string? Hash { get; init; }
    public string? Author { get; init; }
    public string? Email { get; init; }
    public long? Date { get; init; }
    public string? Subject { get; init; }
}
