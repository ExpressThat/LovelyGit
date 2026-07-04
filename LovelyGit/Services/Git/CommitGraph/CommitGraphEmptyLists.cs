using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitGraphEmptyLists
{
    public static readonly List<int> Ints = new(0);
    public static readonly List<int> LaneZero = new(1) { 0 };
    public static readonly List<string> Strings = new(0);
    public static readonly List<CommitLaneColor> LaneColors = new(0);
    public static readonly List<CommitLaneEdge> Edges = new(0);
    public static readonly List<CommitLaneEdge> LaneZeroStraightEdges =
    [
        new CommitLaneEdge
        {
            FromLane = 0,
            ToLane = 0,
            Kind = "straight",
        },
    ];
    public static readonly List<CommitGraphRow> Rows = new(0);
    public static readonly List<CommitRefInfo> Refs = new(0);
}
