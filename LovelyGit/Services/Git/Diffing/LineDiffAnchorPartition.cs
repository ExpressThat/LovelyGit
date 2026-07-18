namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class LineDiffAnchorPartition
{
    private const int MinimumInputLines = 4_000;
    private const int MinimumMismatches = 256;
    private const int MinimumAnchors = 128;

    public static bool TryBuild(
        string[] oldLines,
        string[] newLines,
        IEqualityComparer<string>? comparer,
        int offset,
        out IReadOnlyList<LineDiffBlock> blocks)
    {
        blocks = [];
        if (oldLines.Length + newLines.Length < MinimumInputLines) return false;
        var mismatchCount = CountMismatches(oldLines, newLines, comparer);
        if (mismatchCount < MinimumMismatches)
            return false;

        var anchors = FindAnchors(
            oldLines, newLines, comparer, mismatchCount, out var hasCommonLine);
        if (!hasCommonLine)
        {
            blocks = [new(offset, offset, oldLines.Length, newLines.Length)];
            return true;
        }
        if (anchors.Length < MinimumAnchors) return false;

        var result = new List<LineDiffBlock>(Math.Min(mismatchCount, 4_096));
        var oldStart = 0;
        var newStart = 0;
        foreach (var anchor in anchors)
        {
            AppendGap(
                result,
                oldLines,
                oldStart,
                anchor.OldIndex - oldStart,
                newLines,
                newStart,
                anchor.NewIndex - newStart,
                comparer,
                offset);
            oldStart = anchor.OldIndex + 1;
            newStart = anchor.NewIndex + 1;
        }
        AppendGap(
            result,
            oldLines,
            oldStart,
            oldLines.Length - oldStart,
            newLines,
            newStart,
            newLines.Length - newStart,
            comparer,
            offset);
        blocks = result;
        return true;
    }

    private static int CountMismatches(
        string[] oldLines,
        string[] newLines,
        IEqualityComparer<string>? comparer)
    {
        var mismatches = Math.Abs(oldLines.Length - newLines.Length);
        var length = Math.Min(oldLines.Length, newLines.Length);
        for (var index = 0; index < length; index++)
        {
            if (!Equal(oldLines[index], newLines[index], comparer)) mismatches++;
        }
        return mismatches;
    }

    private static Anchor[] FindAnchors(
        string[] oldLines,
        string[] newLines,
        IEqualityComparer<string>? comparer,
        int mismatchCount,
        out bool hasCommonLine)
    {
        var capacity = Math.Min(
            oldLines.Length + newLines.Length,
            Math.Max(oldLines.Length, newLines.Length) + mismatchCount);
        var occurrences = new Dictionary<string, Occurrence>(
            capacity,
            comparer);
        AddOccurrences(occurrences, oldLines, oldSide: true);
        AddOccurrences(occurrences, newLines, oldSide: false);

        var candidates = new List<Anchor>(Math.Min(oldLines.Length, newLines.Length));
        hasCommonLine = false;
        for (var oldIndex = 0; oldIndex < oldLines.Length; oldIndex++)
        {
            var occurrence = occurrences[oldLines[oldIndex]];
            if (occurrence.NewIndex != 0) hasCommonLine = true;
            if (occurrence.OldIndex > 0 && occurrence.NewIndex > 0)
                candidates.Add(new(oldIndex, occurrence.NewIndex - 1));
        }
        return LongestIncreasingSubsequence(candidates);
    }

    private static void AddOccurrences(
        Dictionary<string, Occurrence> occurrences,
        string[] lines,
        bool oldSide)
    {
        for (var index = 0; index < lines.Length; index++)
        {
            occurrences.TryGetValue(lines[index], out var occurrence);
            if (oldSide)
            {
                occurrence.OldIndex = occurrence.OldIndex == 0 ? index + 1 : -1;
            }
            else
            {
                occurrence.NewIndex = occurrence.NewIndex == 0 ? index + 1 : -1;
            }
            occurrences[lines[index]] = occurrence;
        }
    }

    private static Anchor[] LongestIncreasingSubsequence(List<Anchor> candidates)
    {
        if (candidates.Count == 0) return [];
        var tails = new int[candidates.Count];
        var previous = new int[candidates.Count];
        Array.Fill(previous, -1);
        var length = 0;
        for (var index = 0; index < candidates.Count; index++)
        {
            var low = 0;
            var high = length;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (candidates[tails[middle]].NewIndex < candidates[index].NewIndex) low = middle + 1;
                else high = middle;
            }
            if (low > 0) previous[index] = tails[low - 1];
            tails[low] = index;
            if (low == length) length++;
        }

        var anchors = new Anchor[length];
        var candidateIndex = tails[length - 1];
        for (var index = length - 1; index >= 0; index--)
        {
            anchors[index] = candidates[candidateIndex];
            candidateIndex = previous[candidateIndex];
        }
        return anchors;
    }

    private static void AppendGap(
        List<LineDiffBlock> blocks,
        string[] oldLines,
        int oldStart,
        int oldLength,
        string[] newLines,
        int newStart,
        int newLength,
        IEqualityComparer<string>? comparer,
        int offset)
    {
        if (oldLength == 0 && newLength == 0) return;
        if (oldLength == 0 || newLength == 0 ||
            oldLength == 1 && newLength == 1 &&
            !Equal(oldLines[oldStart], newLines[newStart], comparer))
        {
            blocks.Add(new(oldStart + offset, newStart + offset, oldLength, newLength));
            return;
        }

        var oldGap = oldLines.AsSpan(oldStart, oldLength).ToArray();
        var newGap = newLines.AsSpan(newStart, newLength).ToArray();
        foreach (var edit in new spkl.Diffs.MyersDiff<string>(oldGap, newGap, comparer).GetEditScript())
        {
            blocks.Add(new(
                oldStart + offset + edit.LineA,
                newStart + offset + edit.LineB,
                edit.CountA,
                edit.CountB));
        }
    }

    private static bool Equal(string oldLine, string newLine, IEqualityComparer<string>? comparer) =>
        comparer?.Equals(oldLine, newLine) ?? string.Equals(oldLine, newLine, StringComparison.Ordinal);

    private readonly record struct Anchor(int OldIndex, int NewIndex);

    private struct Occurrence
    {
        public int OldIndex;
        public int NewIndex;
    }
}
