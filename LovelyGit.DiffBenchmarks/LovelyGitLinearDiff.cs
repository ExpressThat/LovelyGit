namespace LovelyGit.DiffBenchmarks;

internal static class LovelyGitLinearDiff
{
    public static CommitFileDiffResponse Run(
        string[] oldLines,
        string[] newLines,
        CommitDiffViewMode viewMode,
        StringComparer comparer)
    {
        var rows = new List<CommitFileDiffLine>(Math.Max(oldLines.Length, newLines.Length));
        var oldIndex = 0;
        var newIndex = 0;
        var hasDifferences = false;
        while (oldIndex < oldLines.Length || newIndex < newLines.Length)
        {
            if (oldIndex >= oldLines.Length)
            {
                AddInserted(rows, ++newIndex, newLines[newIndex - 1], viewMode);
            }
            else if (newIndex >= newLines.Length)
            {
                AddDeleted(rows, ++oldIndex, oldLines[oldIndex - 1], viewMode);
            }
            else if (comparer.Equals(oldLines[oldIndex], newLines[newIndex]))
            {
                AddUnchanged(rows, oldIndex + 1, newIndex + 1, oldLines[oldIndex], viewMode);
                oldIndex++;
                newIndex++;
            }
            else if (CanInsert(oldLines, newLines, oldIndex, newIndex, comparer))
            {
                AddInserted(rows, newIndex + 1, newLines[newIndex], viewMode);
                newIndex++;
                hasDifferences = true;
            }
            else if (CanDelete(oldLines, newLines, oldIndex, newIndex, comparer))
            {
                AddDeleted(rows, oldIndex + 1, oldLines[oldIndex], viewMode);
                oldIndex++;
                hasDifferences = true;
            }
            else
            {
                AddModified(rows, oldIndex + 1, newIndex + 1, oldLines[oldIndex], newLines[newIndex], viewMode);
                oldIndex++;
                newIndex++;
                hasDifferences = true;
            }
        }

        return Response(viewMode, rows, hasDifferences);
    }

    private static bool CanInsert(
        string[] oldLines,
        string[] newLines,
        int oldIndex,
        int newIndex,
        StringComparer comparer)
    {
        return newIndex + 1 < newLines.Length && comparer.Equals(oldLines[oldIndex], newLines[newIndex + 1]);
    }

    private static bool CanDelete(
        string[] oldLines,
        string[] newLines,
        int oldIndex,
        int newIndex,
        StringComparer comparer)
    {
        return oldIndex + 1 < oldLines.Length && comparer.Equals(oldLines[oldIndex + 1], newLines[newIndex]);
    }

    private static void AddUnchanged(
        List<CommitFileDiffLine> rows,
        int oldLine,
        int newLine,
        string text,
        CommitDiffViewMode viewMode)
    {
        rows.Add(viewMode == CommitDiffViewMode.SideBySide
            ? new CommitFileDiffLine
            {
                OldLineNumber = oldLine,
                NewLineNumber = newLine,
                OldText = text,
                NewText = text,
                ChangeType = "Unchanged",
            }
            : new CommitFileDiffLine { OldLineNumber = oldLine, NewLineNumber = newLine, Text = text, ChangeType = "Unchanged" });
    }

    private static void AddInserted(
        List<CommitFileDiffLine> rows,
        int newLine,
        string text,
        CommitDiffViewMode viewMode)
    {
        rows.Add(viewMode == CommitDiffViewMode.SideBySide
            ? new CommitFileDiffLine { NewLineNumber = newLine, NewText = text, ChangeType = "Inserted" }
            : new CommitFileDiffLine { NewLineNumber = newLine, Text = text, ChangeType = "Inserted" });
    }

    private static void AddDeleted(
        List<CommitFileDiffLine> rows,
        int oldLine,
        string text,
        CommitDiffViewMode viewMode)
    {
        rows.Add(viewMode == CommitDiffViewMode.SideBySide
            ? new CommitFileDiffLine { OldLineNumber = oldLine, OldText = text, ChangeType = "Deleted" }
            : new CommitFileDiffLine { OldLineNumber = oldLine, Text = text, ChangeType = "Deleted" });
    }

    private static void AddModified(
        List<CommitFileDiffLine> rows,
        int oldLine,
        int newLine,
        string oldText,
        string newText,
        CommitDiffViewMode viewMode)
    {
        if (viewMode == CommitDiffViewMode.SideBySide)
        {
            rows.Add(new CommitFileDiffLine
            {
                OldLineNumber = oldLine,
                NewLineNumber = newLine,
                OldText = oldText,
                NewText = newText,
                ChangeType = "Modified",
            });
            return;
        }

        AddDeleted(rows, oldLine, oldText, viewMode);
        AddInserted(rows, newLine, newText, viewMode);
    }

    private static CommitFileDiffResponse Response(
        CommitDiffViewMode viewMode,
        List<CommitFileDiffLine> rows,
        bool hasDifferences)
    {
        return new CommitFileDiffResponse
        {
            CommitHash = "LovelyGit Prototype",
            Path = "benchmark.txt",
            Status = "Modified",
            ViewMode = viewMode,
            HasDifferences = hasDifferences,
            Lines = rows,
        };
    }
}
