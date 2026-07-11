using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TypeSharp]
public sealed record CommitArchiveExportResponse
{
    public bool Saved { get; init; }
    public string? Path { get; init; }
}
