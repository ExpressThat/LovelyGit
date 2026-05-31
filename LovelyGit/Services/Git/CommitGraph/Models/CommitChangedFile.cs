using Tapper;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

[TranspilationSource]
public record CommitChangedFile
{
    public string Path { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public uint Additions { get; set; }
    public uint Deletions { get; set; }
    public bool IsBinary { get; set; }
}
