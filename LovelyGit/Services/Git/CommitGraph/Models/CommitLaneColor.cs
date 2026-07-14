using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Models
{
    [TypeSharp]
    public record CommitLaneColor
    {
        public CommitLaneColor()
        {
        }

        public CommitLaneColor(int lane, int colorIndex)
        {
            Lane = lane;
            ColorIndex = colorIndex;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Lane { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int ColorIndex { get; set; }
    }
}
