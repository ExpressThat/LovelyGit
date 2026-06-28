using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitGraphRowBuilder
{
    public static CommitGraphRow Build(
        GitCommit commit,
        IReadOnlyList<string> parents,
        int rowIndex,
        List<string?> activeLaneTargets,
        ref int maxLaneCount,
        string? remoteUrl)
    {
        var hash = commit.Hash.ToString();
        var parentList = parents is List<string> list ? list : parents.ToList();
        var incomingLanes = CommitGraphLaneLayout.FindAllLanesByTarget(activeLaneTargets, hash);
        var activeLanesAbove = rowIndex == 0
            ? []
            : CommitGraphLaneLayout.GetActiveLanes(activeLaneTargets);
        var currentLane = ShouldLandStashLanesOnPrimaryLane(commit, incomingLanes, activeLaneTargets)
            ? 0
            : incomingLanes.Count > 0
            ? incomingLanes[0]
            : IsStashRef(commit)
                ? CommitGraphLaneLayout.AllocateLaneAfter(activeLaneTargets, reservedLane: 0)
                : CommitGraphLaneLayout.AllocateLane(activeLaneTargets);

        foreach (var lane in incomingLanes)
        {
            activeLaneTargets[lane] = null;
        }

        var mainParent = parentList.Count > 0 ? parentList[0] : null;

        if (!string.IsNullOrEmpty(mainParent) && IsStashRef(commit))
        {
            CommitGraphLaneLayout.SetLaneTarget(activeLaneTargets, currentLane, mainParent);
        }
        else if (!string.IsNullOrEmpty(mainParent))
        {
            CommitGraphLaneLayout.SetLaneTarget(activeLaneTargets, currentLane, mainParent);
        }
        else if (currentLane < activeLaneTargets.Count)
        {
            activeLaneTargets[currentLane] = null;
        }

        List<int>? mergeParentLanes = null;
        if (parentList.Count > 1)
        {
            for (var index = 1; index < parentList.Count; index++)
            {
                var parent = parentList[index];
                var parentLane = CommitGraphLaneLayout.FindLaneByTarget(activeLaneTargets, parent)
                    ?? CommitGraphLaneLayout.AllocateLane(activeLaneTargets);
                CommitGraphLaneLayout.SetLaneTarget(activeLaneTargets, parentLane, parent);
                mergeParentLanes ??= new List<int>();
                mergeParentLanes.Add(parentLane);
            }
        }

        CommitGraphLaneLayout.TrimTrailingEmptyLanes(activeLaneTargets);
        maxLaneCount = Math.Max(maxLaneCount, activeLaneTargets.Count);

        var commitInfo = CommitGraphCommitMapper.BuildInfo(commit, parentList, remoteUrl);
        return new CommitGraphRow
        {
            Commit = commitInfo,
            RowIndex = rowIndex,
            Lane = currentLane,
            ActiveLanesAbove = activeLanesAbove,
            ActiveLanesBelow = CommitGraphLaneLayout.GetActiveLanes(activeLaneTargets),
            EdgesAbove = BuildEdgesAbove(incomingLanes, currentLane),
            EdgesBelow = BuildEdgesBelow(currentLane, mainParent, mergeParentLanes),
            IsMergeCommit = parentList.Count > 1,
            IsBranchTip = commitInfo.Branches.Count > 0 || commitInfo.Refs.Any(reference => reference.Kind == CommitRefKind.Stash),
        };
    }

    private static bool IsStashRef(GitCommit commit)
    {
        return commit.Refs.Any(reference => reference.Kind == GitRefKind.Stash);
    }

    private static bool ShouldLandStashLanesOnPrimaryLane(
        GitCommit commit,
        IReadOnlyCollection<int> incomingLanes,
        List<string?> activeLaneTargets)
    {
        return !IsStashRef(commit) &&
            incomingLanes.Count > 0 &&
            incomingLanes.All(lane => lane > 0) &&
            (activeLaneTargets.Count == 0 || activeLaneTargets[0] == null);
    }

    private static List<CommitLaneEdge> BuildEdgesAbove(IEnumerable<int> incomingLanes, int currentLane)
    {
        return incomingLanes
            .Select(lane => new CommitLaneEdge
            {
                FromLane = lane,
                ToLane = currentLane,
                Kind = lane == currentLane ? "straight" : "merge_in",
            })
            .ToList();
    }

    private static List<CommitLaneEdge> BuildEdgesBelow(
        int currentLane,
        string? mainParent,
        IEnumerable<int>? mergeParentLanes)
    {
        var edgesBelow = new List<CommitLaneEdge>();
        if (!string.IsNullOrEmpty(mainParent))
        {
            edgesBelow.Add(new CommitLaneEdge
            {
                FromLane = currentLane,
                ToLane = currentLane,
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
                    Kind = "merge_in",
                });
            }
        }

        return edgesBelow;
    }
}
