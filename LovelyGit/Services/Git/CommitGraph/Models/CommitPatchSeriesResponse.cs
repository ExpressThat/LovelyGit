using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TypeSharp]
public record CommitPatchSeriesResponse
{
    public string Patch { get; set; } = string.Empty;
    public int CommitCount { get; set; }
    public bool IsTruncated { get; set; }
    public bool HasUnsupportedBinaryChanges { get; set; }
}
