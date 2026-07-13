using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

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

    public static bool CanPersistPreparedText(string oldText, string newText)
    {
        if (oldText.Length == 0 || newText.Length == 0) return true;
        return !DiffInputGuard.ShouldUseFastDiff(oldText, newText);
    }
}
