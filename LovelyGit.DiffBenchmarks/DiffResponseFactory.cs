using DiffPlex.DiffBuilder.Model;

namespace LovelyGit.DiffBenchmarks;

internal static class DiffResponseFactory
{
    public static CommitFileDiffResponse FromOperations(
        string candidate,
        CommitDiffViewMode viewMode,
        IReadOnlyList<DiffOperation> operations)
    {
        return new CommitFileDiffResponse
        {
            CommitHash = candidate,
            Path = "benchmark.txt",
            Status = "Modified",
            ViewMode = viewMode,
            HasDifferences = operations.Any(op => op.ChangeType != "Unchanged"),
            Lines = viewMode == CommitDiffViewMode.SideBySide
                ? BuildSideBySide(operations)
                : BuildCombined(operations),
        };
    }

    public static List<CommitFileDiffLine> BuildAddedLines(string[] lines, CommitDiffViewMode viewMode)
    {
        var operations = lines.Select((line, index) =>
            new DiffOperation("Inserted", null, index + 1, null, line)).ToList();
        return viewMode == CommitDiffViewMode.SideBySide ? BuildSideBySide(operations) : BuildCombined(operations);
    }

    public static string[] Lines(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        return text.Contains('\r', StringComparison.Ordinal)
            ? text.ReplaceLineEndings("\n").Split('\n')
            : text.Split('\n');
    }

    private static List<CommitFileDiffLine> BuildSideBySide(IReadOnlyList<DiffOperation> operations)
    {
        var rows = new List<CommitFileDiffLine>(operations.Count);
        foreach (var op in operations)
        {
            rows.Add(new CommitFileDiffLine
            {
                OldLineNumber = op.OldLineNumber,
                NewLineNumber = op.NewLineNumber,
                OldText = op.OldText,
                NewText = op.NewText,
                ChangeType = op.ChangeType,
            });
        }

        return rows;
    }

    private static List<CommitFileDiffLine> BuildCombined(IReadOnlyList<DiffOperation> operations)
    {
        var rows = new List<CommitFileDiffLine>(operations.Count);
        foreach (var op in operations)
        {
            rows.Add(new CommitFileDiffLine
            {
                OldLineNumber = op.OldLineNumber,
                NewLineNumber = op.NewLineNumber,
                Text = op.NewText ?? op.OldText ?? string.Empty,
                ChangeType = op.ChangeType,
            });
        }

        return rows;
    }

    public static string FromDiffPlex(ChangeType changeType)
    {
        return changeType == ChangeType.Unchanged ? "Unchanged" : changeType.ToString();
    }
}

internal sealed record DiffOperation(
    string ChangeType,
    int? OldLineNumber,
    int? NewLineNumber,
    string? OldText,
    string? NewText);
