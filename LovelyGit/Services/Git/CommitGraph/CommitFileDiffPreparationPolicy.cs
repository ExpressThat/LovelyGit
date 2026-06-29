using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitFileDiffPreparationPolicy
{
    public const int MaxPreparedFileCount = 6;

    public static IReadOnlyList<CommitChangedFile> SelectFiles(
        IReadOnlyList<CommitChangedFile> changedFiles)
    {
        if (changedFiles.Count <= MaxPreparedFileCount)
        {
            return changedFiles;
        }

        return changedFiles.Take(MaxPreparedFileCount).ToArray();
    }
}
