using ExpressThat.LovelyGit.Services.TypeGeneration;

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

        public int Lane { get; set; }
        public int ColorIndex { get; set; }
    }
}
