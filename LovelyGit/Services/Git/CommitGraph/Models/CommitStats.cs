namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    public record CommitStats
    {
        public uint Additions { get; set; }
        public uint Deletions { get; set; }
    }
}
