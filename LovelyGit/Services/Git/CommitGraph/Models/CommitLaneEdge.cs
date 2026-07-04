using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    [TypeSharp]
    public record CommitLaneEdge
    {
        public int FromLane { get; set; }
        public int ToLane { get; set; }
        public int ColorIndex { get; set; }
        public string Kind { get; set; } = string.Empty;
    }
}
