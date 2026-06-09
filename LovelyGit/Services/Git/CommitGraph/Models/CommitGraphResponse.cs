using Tapper;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    [TranspilationSource]
    public record CommitGraphResponse
    {
        public int TotalRows { get; set; }
        public int LaneCount { get; set; }
        public List<CommitGraphRow> Rows { get; set; } = new();
        public List<string> RemotePrefixes { get; set; } = new();
        public string? CurrentBranchName { get; set; }
        public string? NextCursor { get; set; }
        public bool HasMore { get; set; }
    }
}
