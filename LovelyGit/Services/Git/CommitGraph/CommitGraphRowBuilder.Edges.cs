using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static partial class CommitGraphRowBuilder
{
    private static List<CommitLaneEdge> BuildEdgesAbove(
        IReadOnlyCollection<int> incomingLanes,
        int currentLane,
        int currentColor,
        IReadOnlyList<CommitLaneColor> laneColorsAbove)
    {
        if (incomingLanes.Count == 0)
        {
            return CommitGraphEmptyLists.Edges;
        }

        if (incomingLanes.Count == 1 && currentLane == 0 && incomingLanes.Contains(0))
        {
            return
            [
                new CommitLaneEdge
                {
                    FromLane = 0,
                    ToLane = 0,
                    ColorIndex = currentColor,
                    Kind = "straight",
                },
            ];
        }

        var edges = new List<CommitLaneEdge>(incomingLanes.Count);
        foreach (var lane in incomingLanes)
        {
            edges.Add(new CommitLaneEdge
            {
                FromLane = lane,
                ToLane = currentLane,
                ColorIndex = ColorForLane(laneColorsAbove, lane, currentColor),
                Kind = lane == currentLane ? "straight" : "merge_in",
            });
        }

        return edges;
    }

    private static List<CommitLaneEdge> BuildEdgesBelow(
        int currentLane,
        int currentColor,
        int? mainParentLane,
        IEnumerable<int>? mergeParentLanes,
        List<int> activeLaneColors)
    {
        if (mainParentLane == null && mergeParentLanes == null)
        {
            return CommitGraphEmptyLists.Edges;
        }

        if (currentLane == 0 && mainParentLane == 0 && mergeParentLanes == null)
        {
            return
            [
                new CommitLaneEdge
                {
                    FromLane = 0,
                    ToLane = 0,
                    ColorIndex = currentColor,
                    Kind = "straight",
                },
            ];
        }

        var edgesBelow = new List<CommitLaneEdge>();
        if (mainParentLane != null)
        {
            edgesBelow.Add(new CommitLaneEdge
            {
                FromLane = currentLane,
                ToLane = mainParentLane.Value,
                ColorIndex = currentColor,
                Kind = "straight",
            });
        }

        if (mergeParentLanes != null)
        {
            foreach (var parentLane in mergeParentLanes)
            {
                edgesBelow.Add(new CommitLaneEdge
                {
                    FromLane = currentLane,
                    ToLane = parentLane,
                    ColorIndex = ColorForLane(activeLaneColors, parentLane),
                    Kind = "merge_in",
                });
            }
        }

        return edgesBelow;
    }
}
