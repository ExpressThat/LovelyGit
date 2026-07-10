using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TypeSharp]
public record CommitPatchExportResponse
{
    public bool Saved { get; set; }
    public string? Path { get; set; }
}
