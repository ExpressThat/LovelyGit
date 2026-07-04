using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static partial class CommitGraphRowBuilder
{
    public static void AdvanceLanes(
        GitCommit commit,
        CommitGraphParentSet parents,
        List<GitObjectId?> activeLaneTargets,
        List<int> activeLaneColors,
        Func<int> allocateColor,
        ref int maxLaneCount)
    {
        var isStash = IsStashRef(commit);
        var incomingLanes = CommitGraphLaneLayout.FindAllLanesByTarget(activeLaneTargets, commit.Hash);
        var currentLane = ShouldLandStashLanesOnPrimaryLane(isStash, incomingLanes, activeLaneTargets)
            ? 0
            : incomingLanes.Count > 0
            ? incomingLanes[0]
            : isStash
                ? CommitGraphLaneLayout.AllocateLaneAfter(activeLaneTargets, reservedLane: 0)
                : CommitGraphLaneLayout.AllocateLane(activeLaneTargets);
        var currentColor = ResolveCurrentColor(
            currentLane,
            incomingLanes,
            activeLaneColors,
            allocateColor);

        foreach (var lane in incomingLanes)
        {
            activeLaneTargets[lane] = null;
            SetLaneColor(activeLaneColors, lane, -1);
        }

        if (parents.Count > 0)
        {
            var mainParent = parents[0];
            if (!parents.IsProcessed(0))
            {
                var existingLane = CommitGraphLaneLayout.FindLaneByTarget(activeLaneTargets, mainParent);
                if (existingLane == null)
                {
                    CommitGraphLaneLayout.SetLaneTarget(activeLaneTargets, currentLane, mainParent);
                    SetLaneColor(activeLaneColors, currentLane, currentColor);
                }
            }
            else if (currentLane < activeLaneTargets.Count)
            {
                activeLaneTargets[currentLane] = null;
                SetLaneColor(activeLaneColors, currentLane, -1);
            }
        }
        else if (currentLane < activeLaneTargets.Count)
        {
            activeLaneTargets[currentLane] = null;
            SetLaneColor(activeLaneColors, currentLane, -1);
        }

        for (var index = 1; index < parents.Count; index++)
        {
            if (parents.IsProcessed(index))
            {
                continue;
            }

            var parent = parents[index];
            var parentLane = CommitGraphLaneLayout.FindLaneByTarget(activeLaneTargets, parent)
                ?? CommitGraphLaneLayout.AllocateLane(activeLaneTargets);
            EnsureLaneHasColor(parentLane, activeLaneColors, allocateColor);
            CommitGraphLaneLayout.SetLaneTarget(activeLaneTargets, parentLane, parent);
        }

        CompactLanes(activeLaneTargets, activeLaneColors);
        maxLaneCount = Math.Max(maxLaneCount, activeLaneTargets.Count);
    }
}
