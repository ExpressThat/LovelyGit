using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static partial class CommitGraphRowBuilder
{
    public static CommitGraphRow Build(
        GitCommit commit,
        CommitGraphParentSet parents,
        int rowIndex,
        List<GitObjectId?> activeLaneTargets,
        List<int> activeLaneColors,
        Func<int> allocateColor,
        ref int maxLaneCount,
        string? remoteUrl = null)
    {
        var isStash = IsStashRef(commit);
        var incomingLanes = CommitGraphLaneLayout.FindAllLanesByTarget(activeLaneTargets, commit.Hash);
        var activeLanesAbove = rowIndex == 0
            ? []
            : CommitGraphLaneLayout.GetActiveLanes(activeLaneTargets);
        var laneColorsAbove = rowIndex == 0
            ? CommitGraphEmptyLists.LaneColors
            : GetActiveLaneColors(activeLaneTargets, activeLaneColors);
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

        int? mainParentLane = null;

        if (parents.Count > 0)
        {
            var mainParent = parents[0];
            if (!parents.IsProcessed(0))
            {
                var existingLane = CommitGraphLaneLayout.FindLaneByTarget(activeLaneTargets, mainParent);
                mainParentLane = existingLane ?? currentLane;
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

        List<int>? mergeParentLanes = null;
        if (parents.Count > 1)
        {
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
                mergeParentLanes ??= new List<int>();
                mergeParentLanes.Add(parentLane);
            }
        }

        var edgesAbove = BuildEdgesAbove(incomingLanes, currentLane, currentColor, laneColorsAbove);
        var edgesBelow = BuildEdgesBelow(
            currentLane,
            currentColor,
            mainParentLane,
            mergeParentLanes,
            activeLaneColors);
        maxLaneCount = Math.Max(
            maxLaneCount,
            Math.Max(activeLaneTargets.Count, currentLane + 1));
        edgesBelow = CompactLanesBelow(activeLaneTargets, activeLaneColors, edgesBelow);
        maxLaneCount = Math.Max(maxLaneCount, activeLaneTargets.Count);

        var commitInfo = CommitGraphCommitMapper.BuildInfo(
            commit,
            BuildParentList(parents),
            remoteUrl);
        return new CommitGraphRow
        {
            Commit = commitInfo,
            RowIndex = rowIndex,
            Lane = currentLane,
            ColorIndex = currentColor,
            ActiveLanesAbove = activeLanesAbove,
            ActiveLanesBelow = CommitGraphLaneLayout.GetActiveLanes(activeLaneTargets),
            LaneColorsAbove = laneColorsAbove,
            LaneColorsBelow = GetActiveLaneColors(activeLaneTargets, activeLaneColors),
            EdgesAbove = edgesAbove,
            EdgesBelow = edgesBelow,
            IsMergeCommit = parents.Count > 1,
            IsBranchTip = HasBranchOrStashRef(commitInfo),
        };
    }

    private static bool IsStashRef(GitCommit commit)
    {
        foreach (var reference in commit.Refs)
        {
            if (reference.Kind == GitRefKind.Stash)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldLandStashLanesOnPrimaryLane(
        bool isStash,
        IReadOnlyCollection<int> incomingLanes,
        List<GitObjectId?> activeLaneTargets)
    {
        if (isStash || incomingLanes.Count == 0 || (activeLaneTargets.Count > 0 && activeLaneTargets[0] != null))
        {
            return false;
        }

        foreach (var lane in incomingLanes)
        {
            if (lane <= 0)
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> BuildParentList(CommitGraphParentSet parents)
    {
        var values = new List<string>(parents.Count);
        for (var index = 0; index < parents.Count; index++)
        {
            values.Add(parents[index].Value);
        }

        return values;
    }

    private static bool HasBranchOrStashRef(CommitInfo commitInfo)
    {
        foreach (var reference in commitInfo.Refs)
        {
            if (reference.Kind is CommitRefKind.Local or CommitRefKind.Remote or CommitRefKind.Stash)
            {
                return true;
            }
        }

        return false;
    }
}
