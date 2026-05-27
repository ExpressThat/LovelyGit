namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitGraphLaneLayout
{
    public static List<int> FindAllLanesByTarget(List<string?> activeLaneTargets, string target)
    {
        var lanes = new List<int>();
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] == target)
            {
                lanes.Add(i);
            }
        }

        return lanes;
    }

    public static int? FindLaneByTarget(List<string?> activeLaneTargets, string target)
    {
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] == target)
            {
                return i;
            }
        }

        return null;
    }

    public static int AllocateLane(List<string?> activeLaneTargets)
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

    public static void SetLaneTarget(List<string?> activeLaneTargets, int lane, string target)
    {
        while (lane >= activeLaneTargets.Count)
        {
            activeLaneTargets.Add(null);
        }

        activeLaneTargets[lane] = target;
    }

    public static void TrimTrailingEmptyLanes(List<string?> activeLaneTargets)
    {
        while (activeLaneTargets.Count > 0 && activeLaneTargets[^1] == null)
        {
            activeLaneTargets.RemoveAt(activeLaneTargets.Count - 1);
        }
    }

    public static List<int> GetActiveLanes(List<string?> activeLaneTargets)
    {
        var lanes = new List<int>();
        for (var i = 0; i < activeLaneTargets.Count; i++)
        {
            if (activeLaneTargets[i] != null)
            {
                lanes.Add(i);
            }
        }

        return lanes;
    }
}
