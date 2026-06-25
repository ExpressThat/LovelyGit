using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    [TypeSharp]
    public record CommitStats
    {
        [TypeAs("number")]
        public uint Additions { get; set; }

        [TypeAs("number")]
        public uint Deletions { get; set; }
    }
}
