using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TypeSharp]
public record CommitChangedFile
{
    public string Path { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    [TypeAs("number")]
    public uint Additions { get; set; }
    [TypeAs("number")]
    public uint Deletions { get; set; }
    public bool IsBinary { get; set; }
}
