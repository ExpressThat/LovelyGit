using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class DiffInputGuard
{
    public const int FastDiffInputCharacters = 300_000;
    public const int FastDiffInputLines = 20_000;
    public const int VirtualTextInputCharacters = 64_000;
    public const int VirtualTextInputLines = 750;

    public static bool ShouldUseVirtualText(string oldText, string newText)
    {
        if ((oldText.Length == 0) == (newText.Length == 0)) return false;
        var text = oldText.Length == 0 ? newText : oldText;
        return text.Length >= VirtualTextInputCharacters
            || CountLines(text) >= VirtualTextInputLines;
    }

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
