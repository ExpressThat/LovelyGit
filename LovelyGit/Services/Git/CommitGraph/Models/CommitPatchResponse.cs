using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TypeSharp]
public record CommitPatchResponse
{
    public string CommitHash { get; set; } = string.Empty;
    public string Patch { get; set; } = string.Empty;
    public string CompactPatchGzipBase64 { get; set; } = string.Empty;
    public bool IsTruncated { get; set; }
    public bool HasUnsupportedBinaryChanges { get; set; }
}
