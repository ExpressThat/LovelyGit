using NetDiff;

namespace LovelyGit.DiffBenchmarks;

internal static class Diff4NetCandidate
{
    public static CommitFileDiffResponse Run(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        var oldLines = DiffResponseFactory.Lines(benchmarkCase.OldText);
        var newLines = DiffResponseFactory.Lines(benchmarkCase.NewText);
        var comparisonOldLines = ignoreWhitespace ? oldLines.Select(NormalizeWhitespace) : oldLines;
        var comparisonNewLines = ignoreWhitespace ? newLines.Select(NormalizeWhitespace) : newLines;
        var results = DiffUtil.Diff(comparisonOldLines, comparisonNewLines, new DiffOption<string>());
        var oldLine = 1;
        var newLine = 1;
        var operations = new List<DiffOperation>();
        foreach (var result in results)
        {
            operations.Add(result.Status switch
            {
                DiffStatus.Inserted => new DiffOperation("Inserted", null, newLine++, null, result.Obj2),
                DiffStatus.Deleted => new DiffOperation("Deleted", oldLine++, null, result.Obj1, null),
                DiffStatus.Modified => new DiffOperation("Modified", oldLine++, newLine++, result.Obj1, result.Obj2),
                _ => new DiffOperation("Unchanged", oldLine++, newLine++, result.Obj1, result.Obj2),
            });
        }

        return DiffResponseFactory.FromOperations("Diff4Net", viewMode, operations);
    }

    private static string NormalizeWhitespace(string line)
    {
        return string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
