using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class DiffInputGuard
{
    public const int MaxDiffInputCharacters = 300_000;
    public const int MaxDiffInputLines = 20_000;

    public static bool ShouldTruncate(string oldText, string newText)
    {
        return oldText.Length + newText.Length > MaxDiffInputCharacters
            || CountLines(oldText) + CountLines(newText) > MaxDiffInputLines;
    }

    public static CommitFileDiffResponse BuildTruncatedResponse(
        string commitHash,
        string path,
        string status,
        CommitDiffViewMode viewMode,
        string oldText,
        string newText)
    {
        return new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = viewMode,
            IsBinary = false,
            HasDifferences = true,
            IsTruncated = true,
            TruncationMessage =
                $"Diff skipped because the file is too large ({DescribeInput(oldText, newText)}).",
        };
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

    private static string DescribeInput(string oldText, string newText)
    {
        var totalCharacters = oldText.Length + newText.Length;
        var totalLines = CountLines(oldText) + CountLines(newText);
        return $"{totalLines:N0} lines, {totalCharacters:N0} characters";
    }
}
