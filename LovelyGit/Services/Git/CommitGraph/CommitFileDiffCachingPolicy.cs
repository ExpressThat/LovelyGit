namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitFileDiffCachingPolicy
{
    public static bool CanUsePersistentCache(int parentIndex, bool ignoreWhitespace)
    {
        _ = ignoreWhitespace;
        return parentIndex == 0;
    }
}
