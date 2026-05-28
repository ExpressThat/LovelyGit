using Tapper;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    [TranspilationSource]
    public record CommitLaneEdge
    {
        public int FromLane { get; set; }
        public int ToLane { get; set; }
        public string Kind { get; set; } = string.Empty;
    }
}
