using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    [TypeSharp]
    public record CommitStats
    {
        public uint Additions { get; set; }
        public uint Deletions { get; set; }
    }
}
