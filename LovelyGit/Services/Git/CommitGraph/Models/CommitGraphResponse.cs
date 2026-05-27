namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    public record CommitGraphResponse
    {
        public int TotalRows { get; set; }
        public int LaneCount { get; set; }
        public List<CommitGraphRow> Rows { get; set; } = new();
        public string? NextCursor { get; set; }
        public bool HasMore { get; set; }
    }
}
