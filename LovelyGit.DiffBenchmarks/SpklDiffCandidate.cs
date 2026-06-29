using spkl.Diffs;

namespace LovelyGit.DiffBenchmarks;

internal static class SpklDiffCandidate
{
    public static CommitFileDiffResponse Run(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        var oldLines = DiffResponseFactory.Lines(benchmarkCase.OldText);
        var newLines = DiffResponseFactory.Lines(benchmarkCase.NewText);
        var comparer = ignoreWhitespace ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var diff = new MyersDiff<string>(oldLines, newLines, comparer);
        var operations = new List<DiffOperation>();
        var oldLine = 1;
        var newLine = 1;
        foreach (var item in diff.GetResult(ResultOrder.AABB))
        {
            operations.Add(item.Item1 switch
            {
                ResultType.A => new DiffOperation("Deleted", oldLine++, null, item.Item2, null),
                ResultType.B => new DiffOperation("Inserted", null, newLine++, null, item.Item3),
                _ => new DiffOperation("Unchanged", oldLine++, newLine++, item.Item2, item.Item3),
            });
        }

        return DiffResponseFactory.FromOperations("spkl.Diffs", viewMode, operations);
    }
}
