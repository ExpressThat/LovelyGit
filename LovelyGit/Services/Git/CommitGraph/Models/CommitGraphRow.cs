using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    [TypeSharp]
    public record CommitGraphRow
    {
        public CommitInfo Commit { get; set; } = new();
        public int RowIndex { get; set; }
        public int Lane { get; set; }
        public int ColorIndex { get; set; }
        public List<int> ActiveLanesAbove { get; set; } = new();
        public List<int> ActiveLanesBelow { get; set; } = new();
        public List<CommitLaneColor> LaneColorsAbove { get; set; } = new();
        public List<CommitLaneColor> LaneColorsBelow { get; set; } = new();
        public List<CommitLaneEdge> EdgesAbove { get; set; } = new();
        public List<CommitLaneEdge> EdgesBelow { get; set; } = new();
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsMergeCommit { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsBranchTip { get; set; }
    }
}
