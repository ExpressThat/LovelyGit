namespace LovelyGit.DiffBenchmarks;

internal static class LovelyGitFallbackDiff
{
    private const int MaxExactMiddleLines = 20_000;

    public static CommitFileDiffResponse Run(
        string[] oldLines,
        string[] newLines,
        CommitDiffViewMode viewMode,
        StringComparer comparer)
    {
        var prefix = CommonPrefix(oldLines, newLines, comparer);
        var suffix = CommonSuffix(oldLines, newLines, prefix, comparer);
        var operations = new List<DiffOperation>(Math.Max(oldLines.Length, newLines.Length));
        AddUnchanged(operations, oldLines, newLines, 0, 0, prefix);
        AddMiddle(operations, oldLines, newLines, prefix, suffix, comparer);
        AddUnchanged(
            operations,
            oldLines,
            newLines,
            oldLines.Length - suffix,
            newLines.Length - suffix,
            suffix);
        return DiffResponseFactory.FromOperations("LovelyGit Prototype", viewMode, operations);
    }

    private static void AddMiddle(
        List<DiffOperation> operations,
        string[] oldLines,
        string[] newLines,
        int prefix,
        int suffix,
        StringComparer comparer)
    {
        var oldCount = oldLines.Length - prefix - suffix;
        var newCount = newLines.Length - prefix - suffix;
        if (oldCount + newCount <= MaxExactMiddleLines)
        {
            AddExactMiddle(operations, oldLines, newLines, prefix, oldCount, newCount, comparer);
            return;
        }

        for (var index = 0; index < oldCount; index++)
        {
            operations.Add(new DiffOperation("Deleted", prefix + index + 1, null, oldLines[prefix + index], null));
        }

        for (var index = 0; index < newCount; index++)
        {
            operations.Add(new DiffOperation("Inserted", null, prefix + index + 1, null, newLines[prefix + index]));
        }
    }

    private static void AddExactMiddle(
        List<DiffOperation> operations,
        string[] oldLines,
        string[] newLines,
        int offset,
        int oldCount,
        int newCount,
        StringComparer comparer)
    {
        var oldSlice = oldLines.AsSpan(offset, oldCount).ToArray();
        var newSlice = newLines.AsSpan(offset, newCount).ToArray();
        var diff = MyersDiff.Algorithm.ComputeDiff(oldSlice.AsSpan(), newSlice.AsSpan(), comparer);
        MyersDiff.Algorithm.ReorderDeletesBeforeInserts(diff);
        foreach (var op in diff)
        {
            operations.Add(op switch
            {
                MyersDiff.Diff.Delete delete => new DiffOperation("Deleted", offset + delete.X, null, oldSlice[delete.X - 1], null),
                MyersDiff.Diff.Insert insert => new DiffOperation("Inserted", null, offset + insert.Y, null, newSlice[insert.Y - 1]),
                MyersDiff.Diff.Equal equal => new DiffOperation("Unchanged", offset + equal.X, offset + equal.Y, oldSlice[equal.X - 1], newSlice[equal.Y - 1]),
                _ => throw new InvalidOperationException("Unknown operation."),
            });
        }
    }

    private static void AddUnchanged(
        List<DiffOperation> operations,
        string[] oldLines,
        string[] newLines,
        int oldStart,
        int newStart,
        int count)
    {
        for (var index = 0; index < count; index++)
        {
            operations.Add(new DiffOperation(
                "Unchanged",
                oldStart + index + 1,
                newStart + index + 1,
                oldLines[oldStart + index],
                newLines[newStart + index]));
        }
    }

    private static int CommonPrefix(string[] oldLines, string[] newLines, StringComparer comparer)
    {
        var length = Math.Min(oldLines.Length, newLines.Length);
        var index = 0;
        while (index < length && comparer.Equals(oldLines[index], newLines[index]))
        {
            index++;
        }

        return index;
    }

    private static int CommonSuffix(string[] oldLines, string[] newLines, int prefix, StringComparer comparer)
    {
        var max = Math.Min(oldLines.Length, newLines.Length) - prefix;
        var index = 0;
        while (index < max
            && comparer.Equals(oldLines[oldLines.Length - index - 1], newLines[newLines.Length - index - 1]))
        {
            index++;
        }

        return index;
    }
}
