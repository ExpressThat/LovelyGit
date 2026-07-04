using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static partial class CommitGraphRowBuilder
{
    private static List<CommitLaneEdge> CompactLanesBelow(
        List<GitObjectId?> activeLaneTargets,
        List<int> activeLaneColors,
        List<CommitLaneEdge> edgesBelow)
    {
        var movedColors = new Dictionary<int, int>();
        var laneMap = CompactLanes(activeLaneTargets, activeLaneColors, movedColors);
        if (laneMap.Count == 0)
        {
            return edgesBelow;
        }

        var edges = edgesBelow.Count == 0
            ? new List<CommitLaneEdge>(laneMap.Count)
            : new List<CommitLaneEdge>(edgesBelow.Count + laneMap.Count);
        var explicitFromLanes = new HashSet<int>();
        foreach (var edge in edgesBelow)
        {
            explicitFromLanes.Add(edge.FromLane);
            edges.Add(edge with
            {
                ToLane = laneMap.GetValueOrDefault(edge.ToLane, edge.ToLane),
            });
        }

        foreach (var (fromLane, toLane) in laneMap)
        {
            if (fromLane == toLane || explicitFromLanes.Contains(fromLane))
            {
                continue;
            }

            edges.Add(new CommitLaneEdge
            {
                FromLane = fromLane,
                ToLane = toLane,
                ColorIndex = movedColors.GetValueOrDefault(
                    fromLane,
                    ColorForLane(activeLaneColors, toLane)),
                Kind = "compact",
            });
        }

        return edges.Count == 0 ? CommitGraphEmptyLists.Edges : edges;
    }

    private static Dictionary<int, int> CompactLanes(
        List<GitObjectId?> activeLaneTargets,
        List<int> activeLaneColors,
        Dictionary<int, int>? movedColors = null)
    {
        var laneMap = new Dictionary<int, int>();
        var writeIndex = 0;
        for (var readIndex = 0; readIndex < activeLaneTargets.Count; readIndex++)
        {
            var target = activeLaneTargets[readIndex];
            if (target == null)
            {
                continue;
            }

            if (writeIndex != readIndex)
            {
                laneMap[readIndex] = writeIndex;
            }

            var originalColor = ColorForLane(activeLaneColors, readIndex);
            if (writeIndex != readIndex)
            {
                SetLaneColor(activeLaneColors, readIndex, -1);
            }

            var color = AvoidNeighborColor(activeLaneColors, writeIndex, originalColor);
            activeLaneTargets[writeIndex] = target.Value;
            SetLaneColor(activeLaneColors, writeIndex, color);
            movedColors?.Add(readIndex, color);
            if (writeIndex != readIndex)
            {
                activeLaneTargets[readIndex] = null;
            }

            writeIndex++;
        }

        if (writeIndex < activeLaneTargets.Count)
        {
            activeLaneTargets.RemoveRange(writeIndex, activeLaneTargets.Count - writeIndex);
        }

        if (writeIndex < activeLaneColors.Count)
        {
            activeLaneColors.RemoveRange(writeIndex, activeLaneColors.Count - writeIndex);
        }

        return laneMap;
    }
}
