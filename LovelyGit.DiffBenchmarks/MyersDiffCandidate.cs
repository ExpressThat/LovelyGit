using MyersDiff;

namespace LovelyGit.DiffBenchmarks;

internal static class MyersDiffCandidate
{
    public static CommitFileDiffResponse Run(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        var oldLines = DiffResponseFactory.Lines(benchmarkCase.OldText);
        var newLines = DiffResponseFactory.Lines(benchmarkCase.NewText);
        var comparer = ignoreWhitespace ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var diff = Algorithm.ComputeDiff(oldLines.AsSpan(), newLines.AsSpan(), comparer);
        Algorithm.ReorderDeletesBeforeInserts(diff);
        var operations = new List<DiffOperation>(diff.Count);
        foreach (var op in diff)
        {
            operations.Add(op switch
            {
                Diff.Delete delete => new DiffOperation("Deleted", delete.X, null, oldLines[delete.X - 1], null),
                Diff.Insert insert => new DiffOperation("Inserted", null, insert.Y, null, newLines[insert.Y - 1]),
                Diff.Equal equal => new DiffOperation("Unchanged", equal.X, equal.Y, oldLines[equal.X - 1], newLines[equal.Y - 1]),
                _ => throw new InvalidOperationException("Unknown MyersDiff operation."),
            });
        }

        return DiffResponseFactory.FromOperations("MyersDiff", viewMode, operations);
    }
}
