using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitGraphLaneLayout
{
    public static List<int> FindAllLanesByTarget(List<GitObjectId?> activeLaneTargets, GitObjectId target)
    {
        List<int>? lanes = null;
        var laneZeroMatched = false;
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] is { } candidate && candidate.Equals(target))
            {
                if (i == 0 && lanes == null)
                {
                    laneZeroMatched = true;
                    continue;
                }

                if (laneZeroMatched)
                {
                    lanes = new List<int> { 0 };
                    laneZeroMatched = false;
                }

                lanes ??= new List<int>();
                lanes.Add(i);
            }
        }

        return lanes ?? (laneZeroMatched ? CommitGraphEmptyLists.LaneZero : CommitGraphEmptyLists.Ints);
    }

    public static int? FindLaneByTarget(List<GitObjectId?> activeLaneTargets, GitObjectId target)
    {
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] is { } candidate && candidate.Equals(target))
            {
                return i;
            }
        }

        return null;
    }

    public static int AllocateLane(List<GitObjectId?> activeLaneTargets)
    {
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] == null)
            {
                return i;
            }
        }

        activeLaneTargets.Add(null);
        return activeLaneTargets.Count - 1;
    }

    public static int AllocateLaneAfter(
        List<GitObjectId?> activeLaneTargets,
        int reservedLane)
    {
        while (activeLaneTargets.Count <= reservedLane)
        {
            activeLaneTargets.Add(null);
        }

        for (var i = reservedLane + 1; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] == null)
            {
                return i;
            }
        }

        activeLaneTargets.Add(null);
        return activeLaneTargets.Count - 1;
    }

    public static void SetLaneTarget(List<GitObjectId?> activeLaneTargets, int lane, GitObjectId target)
    {
        while (lane >= activeLaneTargets.Count)
        {
            activeLaneTargets.Add(null);
        }

        activeLaneTargets[lane] = target;
    }

    public static void TrimTrailingEmptyLanes(List<GitObjectId?> activeLaneTargets)
    {
        while (activeLaneTargets.Count > 0 && activeLaneTargets[^1] == null)
        {
            activeLaneTargets.RemoveAt(activeLaneTargets.Count - 1);
        }
    }

    public static List<int> GetActiveLanes(List<GitObjectId?> activeLaneTargets)
    {
        List<int>? lanes = null;
        var laneZeroActive = false;
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] != null)
            {
                if (i == 0 && lanes == null)
                {
                    laneZeroActive = true;
                    continue;
                }

                if (laneZeroActive)
                {
                    lanes = new List<int> { 0 };
                    laneZeroActive = false;
                }

                lanes ??= new List<int>();
                lanes.Add(i);
            }
        }

        return lanes ?? (laneZeroActive ? CommitGraphEmptyLists.LaneZero : CommitGraphEmptyLists.Ints);
    }
}
