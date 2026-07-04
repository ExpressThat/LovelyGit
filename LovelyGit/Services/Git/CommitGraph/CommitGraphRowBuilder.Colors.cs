using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static partial class CommitGraphRowBuilder
{
    private const int ColorSlotCount = 10;

    private static int ResolveCurrentColor(
        int currentLane,
        IReadOnlyList<int> incomingLanes,
        List<int> activeLaneColors,
        Func<int> allocateColor)
    {
        if (incomingLanes.Count > 0)
        {
            return ColorForLane(activeLaneColors, incomingLanes[0]);
        }

        var color = AllocateFlowColor(currentLane, activeLaneColors, allocateColor);
        SetLaneColor(activeLaneColors, currentLane, color);
        return color;
    }

    private static void EnsureLaneHasColor(
        int lane,
        List<int> activeLaneColors,
        Func<int> allocateColor)
    {
        if (ColorForLane(activeLaneColors, lane) >= 0)
        {
            return;
        }

        SetLaneColor(activeLaneColors, lane, AllocateFlowColor(lane, activeLaneColors, allocateColor));
    }

    private static int AllocateFlowColor(
        int lane,
        List<int> activeLaneColors,
        Func<int> allocateColor)
    {
        var first = Math.Abs(allocateColor()) % ColorSlotCount;
        for (var offset = 0; offset < ColorSlotCount; offset++)
        {
            var candidate = (first + offset) % ColorSlotCount;
            if (!NeighborHasColor(activeLaneColors, lane, candidate))
            {
                return candidate;
            }
        }

        return first;
    }

    private static bool NeighborHasColor(List<int> activeLaneColors, int lane, int color)
    {
        return ColorForLane(activeLaneColors, lane - 1) == color
            || ColorForLane(activeLaneColors, lane + 1) == color;
    }

    private static int AvoidNeighborColor(List<int> activeLaneColors, int lane, int preferred)
    {
        if (!NeighborHasColor(activeLaneColors, lane, preferred))
        {
            return preferred;
        }

        for (var offset = 1; offset < ColorSlotCount; offset++)
        {
            var candidate = (preferred + offset) % ColorSlotCount;
            if (!NeighborHasColor(activeLaneColors, lane, candidate))
            {
                return candidate;
            }
        }

        return preferred;
    }

    private static List<CommitLaneColor> GetActiveLaneColors(
        List<GitObjectId?> activeLaneTargets,
        List<int> activeLaneColors)
    {
        List<CommitLaneColor>? colors = null;
        for (var lane = 0; lane < activeLaneTargets.Count; lane++)
        {
            if (activeLaneTargets[lane] == null)
            {
                continue;
            }

            colors ??= new List<CommitLaneColor>();
            colors.Add(new CommitLaneColor(lane, ColorForLane(activeLaneColors, lane)));
        }

        return colors ?? CommitGraphEmptyLists.LaneColors;
    }

    private static int ColorForLane(IReadOnlyList<CommitLaneColor> laneColors, int lane, int fallback)
    {
        for (var i = 0; i < laneColors.Count; i++)
        {
            if (laneColors[i].Lane == lane)
            {
                return laneColors[i].ColorIndex;
            }
        }

        return fallback;
    }

    private static int ColorForLane(List<int> activeLaneColors, int lane)
    {
        return lane >= 0 && lane < activeLaneColors.Count ? activeLaneColors[lane] : -1;
    }

    private static void SetLaneColor(List<int> activeLaneColors, int lane, int color)
    {
        while (activeLaneColors.Count <= lane)
        {
            activeLaneColors.Add(-1);
        }

        activeLaneColors[lane] = color;
    }
}
