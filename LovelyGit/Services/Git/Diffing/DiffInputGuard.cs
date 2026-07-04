using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class DiffInputGuard
{
    public const int FastDiffInputCharacters = 300_000;
    public const int FastDiffInputLines = 20_000;

    public static bool ShouldUseFastDiff(string oldText, string newText)
    {
        return oldText.Length + newText.Length > FastDiffInputCharacters
            || CountLines(oldText) + CountLines(newText) > FastDiffInputLines;
    }

    private static int CountLines(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        var count = 1;
        foreach (var character in text)
        {
            if (character == '\n')
            {
                count++;
            }
        }

        return count;
    }

}
