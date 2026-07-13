namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class LineDiffEngine
{
    public static LineDiffModel Build(string oldText, string newText, bool ignoreWhitespace = false)
        => Build(Prepare(oldText), Prepare(newText), ignoreWhitespace);

    public static LineDiffModel BuildUnaligned(string oldText, string newText, bool ignoreWhitespace = false)
        => BuildCore(Prepare(oldText), Prepare(newText), ignoreWhitespace, alignRows: false);

    public static PreparedLineText Prepare(string text) =>
        new(SplitLines(text), EndsWithNewLine(text));

    public static LineDiffModel Build(
        PreparedLineText oldText,
        PreparedLineText newText,
        bool ignoreWhitespace = false)
        => BuildCore(oldText, newText, ignoreWhitespace, alignRows: true);

    private static LineDiffModel BuildCore(
        PreparedLineText oldText,
        PreparedLineText newText,
        bool ignoreWhitespace,
        bool alignRows)
    {
        var endingsDiffer = oldText.EndsWithNewLine != newText.EndsWithNewLine;
        var comparisonOldLines = ComparisonLines(oldText, endingsDiffer);
        var comparisonNewLines = ComparisonLines(newText, endingsDiffer);
        var comparer = ignoreWhitespace ? WhitespaceIgnoringLineComparer.Instance : null;
        var edits = new spkl.Diffs.MyersDiff<string>(comparisonOldLines, comparisonNewLines, comparer)
            .GetEditScript()
            .Select(edit => new LineDiffBlock(edit.Item1, edit.Item2, edit.Item3, edit.Item4))
            .ToArray();
        return new LineDiffModel(
            oldText.Lines,
            newText.Lines,
            edits,
            alignRows ? Align(oldText.Lines.Length, newText.Lines.Length, edits) : []);
    }

    private static string[] ComparisonLines(PreparedLineText text, bool endingsDiffer)
    {
        if (!endingsDiffer || text.Lines.Length == 0 || !text.EndsWithNewLine) return text.Lines;
        var comparison = (string[])text.Lines.Clone();
        comparison[^1] += "\n";
        return comparison;
    }

    private static bool EndsWithNewLine(string text) => text.EndsWith('\n') || text.EndsWith('\r');

    public static string[] SplitLines(string text)
    {
        if (text.Length == 0) return [];
        var separatorCount = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] is not ('\r' or '\n')) continue;
            separatorCount++;
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
        }

        var endsWithNewLine = EndsWithNewLine(text);
        var lines = new string[separatorCount + (endsWithNewLine ? 0 : 1)];
        var lineStart = 0;
        var lineIndex = 0;
        for (var index = 0; index < text.Length && lineIndex < lines.Length; index++)
        {
            if (text[index] is not ('\r' or '\n')) continue;
            lines[lineIndex++] = text.Substring(lineStart, index - lineStart);
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
            lineStart = index + 1;
        }
        if (!endsWithNewLine) lines[^1] = text[lineStart..];
        return lines;
    }

    private static List<LineDiffRow> Align(int oldCount, int newCount, IReadOnlyList<LineDiffBlock> edits)
    {
        var rows = new List<LineDiffRow>(Math.Max(oldCount, newCount));
        rows.AddRange(EnumerateRows(oldCount, newCount, edits));
        return rows;
    }

    public static IEnumerable<LineDiffRow> EnumerateRows(LineDiffModel model) =>
        EnumerateRows(model.OldLines.Length, model.NewLines.Length, model.Blocks);

    private static IEnumerable<LineDiffRow> EnumerateRows(
        int oldCount,
        int newCount,
        IReadOnlyList<LineDiffBlock> edits)
    {
        var oldIndex = 0;
        var newIndex = 0;
        foreach (var edit in edits)
        {
            while (oldIndex < edit.OldStart && newIndex < edit.NewStart)
                yield return new(oldIndex++, newIndex++, isChanged: false);
            var count = Math.Max(edit.OldCount, edit.NewCount);
            for (var offset = 0; offset < count; offset++)
            {
                yield return new(
                    offset < edit.OldCount ? edit.OldStart + offset : null,
                    offset < edit.NewCount ? edit.NewStart + offset : null,
                    isChanged: true);
            }
            oldIndex = edit.OldStart + edit.OldCount;
            newIndex = edit.NewStart + edit.NewCount;
        }
        while (oldIndex < oldCount && newIndex < newCount)
            yield return new(oldIndex++, newIndex++, isChanged: false);
    }
}

internal readonly record struct PreparedLineText(string[] Lines, bool EndsWithNewLine);

internal sealed record LineDiffModel(
    string[] OldLines,
    string[] NewLines,
    IReadOnlyList<LineDiffBlock> Blocks,
    IReadOnlyList<LineDiffRow> Rows)
{
    public bool HasDifferences => Blocks.Count > 0;
}

internal readonly record struct LineDiffBlock(int OldStart, int NewStart, int OldCount, int NewCount);
internal readonly record struct LineDiffRow
{
    private const int MissingIndex = int.MaxValue;
    private const uint ChangedMask = 1u << 31;
    private const uint IndexMask = ChangedMask - 1;

    private readonly int oldIndex;
    private readonly uint newIndexAndFlags;

    public LineDiffRow(int? oldIndex, int? newIndex, bool isChanged)
    {
        this.oldIndex = oldIndex ?? MissingIndex;
        newIndexAndFlags = (uint)(newIndex ?? MissingIndex);
        if (isChanged) newIndexAndFlags |= ChangedMask;
    }

    public int? OldIndex => oldIndex == MissingIndex ? null : oldIndex;
    public int? NewIndex
    {
        get
        {
            var value = newIndexAndFlags & IndexMask;
            return value == MissingIndex ? null : (int)value;
        }
    }

    public bool IsChanged => (newIndexAndFlags & ChangedMask) != 0;
}
