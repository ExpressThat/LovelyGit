using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictHunkRangeMapper
{
    public static LineRange Map(LineDiffModel model, int sourceStart, int sourceCount)
    {
        var first = FindNewLineAtOrAfter(model.Rows, sourceStart);
        if (sourceCount == 0)
        {
            if (first < 0) first = model.Rows.Count;
            var deletionStart = first;
            while (deletionStart > 0 && model.Rows[deletionStart - 1].NewIndex is null) deletionStart--;
            return deletionStart == first
                ? new LineRange(sourceStart, 0)
                : RangeFromAlignedLines(model.Rows, deletionStart, first - 1, sourceStart);
        }

        if (first < 0) return new LineRange(sourceStart, 0);
        var last = first;
        var sourceEnd = sourceStart + sourceCount;
        while (last + 1 < model.Rows.Count &&
               model.Rows[last + 1].NewIndex is { } next && next < sourceEnd)
        {
            last++;
        }
        while (first > 0 && model.Rows[first - 1].NewIndex is null) first--;
        while (last + 1 < model.Rows.Count && model.Rows[last + 1].NewIndex is null) last++;
        return RangeFromAlignedLines(model.Rows, first, last, sourceStart);
    }

    public static LineRange Union(LineRange left, LineRange right)
    {
        if (left.Count == 0) return right;
        if (right.Count == 0) return left;
        var start = Math.Min(left.Start, right.Start);
        var end = Math.Max(left.Start + left.Count, right.Start + right.Count);
        return new LineRange(start, end - start);
    }

    private static int FindNewLineAtOrAfter(IReadOnlyList<LineDiffRow> lines, int target)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].NewIndex is { } position && position >= target) return index;
        }
        return -1;
    }

    private static LineRange RangeFromAlignedLines(
        IReadOnlyList<LineDiffRow> lines,
        int first,
        int last,
        int fallbackStart)
    {
        var start = -1;
        var end = -1;
        for (var index = first; index <= last; index++)
        {
            if (lines[index].OldIndex is not { } oldIndex) continue;
            if (start < 0) start = oldIndex;
            end = oldIndex;
        }
        return start < 0
            ? new LineRange(fallbackStart, 0)
            : new LineRange(start, end - start + 1);
    }

    internal readonly record struct LineRange(int Start, int Count);
}
