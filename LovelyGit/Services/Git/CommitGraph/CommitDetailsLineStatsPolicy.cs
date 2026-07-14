namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitDetailsLineStatsPolicy
{
    internal const int MaxFiles = 500;

    public static bool ShouldCalculate(int changedFileCount)
    {
        return changedFileCount <= MaxFiles;
    }
}
