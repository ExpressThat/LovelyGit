using DiffPlex.DiffBuilder.Model;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictHunkRangeMapper
{
    public static LineRange Map(SideBySideDiffModel model, int sourceStart, int sourceCount)
    {
        return sourceCount == 0
            ? MapEmptyCandidate(model, sourceStart)
            : MapCandidate(model, sourceStart, sourceCount);
    }

    public static LineRange Union(LineRange left, LineRange right)
    {
        if (left.Count == 0) return right;
        if (right.Count == 0) return left;
        var start = Math.Min(left.Start, right.Start);
        var end = Math.Max(left.Start + left.Count, right.Start + right.Count);
        return new LineRange(start, end - start);
    }

    private static LineRange MapEmptyCandidate(SideBySideDiffModel model, int sourceStart)
    {
        var lines = model.NewText.Lines;
        var anchor = FindPositionAtOrAfter(lines, sourceStart + 1);
        if (anchor < 0) anchor = lines.Count;
        var first = anchor;
        while (first > 0 && lines[first - 1].Type == ChangeType.Imaginary)
        {
            first--;
        }

        return first == anchor
            ? new LineRange(sourceStart, 0)
            : RangeFromOldLines(model, first, anchor - 1, sourceStart);
    }

    private static LineRange MapCandidate(SideBySideDiffModel model, int sourceStart, int sourceCount)
    {
        var lines = model.NewText.Lines;
        var first = FindPositionAtOrAfter(lines, sourceStart + 1);
        if (first < 0) return new LineRange(sourceStart, 0);
        var last = first;
        var sourceEnd = sourceStart + sourceCount;
        while (last + 1 < lines.Count &&
               lines[last + 1].Position is { } position &&
               position <= sourceEnd)
        {
            last++;
        }

        while (first > 0 && lines[first - 1].Type == ChangeType.Imaginary) first--;
        while (last + 1 < lines.Count && lines[last + 1].Type == ChangeType.Imaginary) last++;
        return RangeFromOldLines(model, first, last, sourceStart);
    }

    private static LineRange RangeFromOldLines(
        SideBySideDiffModel model,
        int first,
        int last,
        int fallbackStart)
    {
        var positions = model.OldText.Lines
            .Skip(first)
            .Take(last - first + 1)
            .Where(line => line.Position.HasValue)
            .Select(line => line.Position!.Value - 1)
            .ToArray();
        return positions.Length == 0
            ? new LineRange(fallbackStart, 0)
            : new LineRange(positions.Min(), positions.Max() - positions.Min() + 1);
    }

    private static int FindPositionAtOrAfter(IReadOnlyList<DiffPiece> lines, int target)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].Position is { } position && position >= target) return index;
        }

        return -1;
    }

    internal readonly record struct LineRange(int Start, int Count);
}
