namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class LineDiffEngine
{
    public static LineDiffModel Build(string oldText, string newText, bool ignoreWhitespace = false)
    {
        var oldLines = SplitLines(oldText);
        var newLines = SplitLines(newText);
        var endingsDiffer = EndsWithNewLine(oldText) != EndsWithNewLine(newText);
        var comparisonOldLines = ComparisonLines(oldLines, oldText, endingsDiffer);
        var comparisonNewLines = ComparisonLines(newLines, newText, endingsDiffer);
        var comparer = ignoreWhitespace ? WhitespaceIgnoringLineComparer.Instance : null;
        var edits = new spkl.Diffs.MyersDiff<string>(comparisonOldLines, comparisonNewLines, comparer)
            .GetEditScript()
            .Select(edit => new LineDiffBlock(edit.Item1, edit.Item2, edit.Item3, edit.Item4))
            .ToArray();
        return new LineDiffModel(oldLines, newLines, edits, Align(oldLines.Length, newLines.Length, edits));
    }

    private static string[] ComparisonLines(string[] lines, string text, bool endingsDiffer)
    {
        if (!endingsDiffer || lines.Length == 0 || !EndsWithNewLine(text)) return lines;
        var comparison = (string[])lines.Clone();
        comparison[^1] += "\n";
        return comparison;
    }

    private static bool EndsWithNewLine(string text) => text.EndsWith('\n') || text.EndsWith('\r');

    public static string[] SplitLines(string text)
    {
        if (text.Length == 0) return [];
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n');
        return normalized.EndsWith('\n') ? lines[..^1] : lines;
    }

    private static List<LineDiffRow> Align(int oldCount, int newCount, IReadOnlyList<LineDiffBlock> edits)
    {
        var rows = new List<LineDiffRow>(Math.Max(oldCount, newCount));
        var oldIndex = 0;
        var newIndex = 0;
        foreach (var edit in edits)
        {
            AddUnchanged(rows, ref oldIndex, ref newIndex, edit.OldStart, edit.NewStart);
            var count = Math.Max(edit.OldCount, edit.NewCount);
            for (var offset = 0; offset < count; offset++)
            {
                rows.Add(new(
                    offset < edit.OldCount ? edit.OldStart + offset : null,
                    offset < edit.NewCount ? edit.NewStart + offset : null,
                    IsChanged: true));
            }
            oldIndex = edit.OldStart + edit.OldCount;
            newIndex = edit.NewStart + edit.NewCount;
        }
        AddUnchanged(rows, ref oldIndex, ref newIndex, oldCount, newCount);
        return rows;
    }

    private static void AddUnchanged(
        List<LineDiffRow> rows,
        ref int oldIndex,
        ref int newIndex,
        int oldEnd,
        int newEnd)
    {
        while (oldIndex < oldEnd && newIndex < newEnd)
            rows.Add(new(oldIndex++, newIndex++, IsChanged: false));
    }
}

internal sealed record LineDiffModel(
    string[] OldLines,
    string[] NewLines,
    IReadOnlyList<LineDiffBlock> Blocks,
    IReadOnlyList<LineDiffRow> Rows)
{
    public bool HasDifferences => Blocks.Count > 0;
}

internal readonly record struct LineDiffBlock(int OldStart, int NewStart, int OldCount, int NewCount);
internal readonly record struct LineDiffRow(int? OldIndex, int? NewIndex, bool IsChanged);
