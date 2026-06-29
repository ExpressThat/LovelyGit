using CSharpDiff.Diffs;
using CSharpDiff.Diffs.Models;

namespace LovelyGit.DiffBenchmarks;

internal static class CSharpDiffCandidate
{
    public static CommitFileDiffResponse Run(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        var diff = new DiffLines(new DiffOptions { IgnoreWhiteSpace = ignoreWhitespace });
        var results = diff.diff(benchmarkCase.OldText, benchmarkCase.NewText);
        var oldLine = 1;
        var newLine = 1;
        var operations = new List<DiffOperation>();
        foreach (var result in results)
        {
            var lines = result.lines is { Length: > 0 } ? result.lines : [result.value];
            foreach (var line in lines.Where(line => line != string.Empty))
            {
                operations.Add(ToOperation(result, line, ref oldLine, ref newLine));
            }
        }

        return DiffResponseFactory.FromOperations("CSharpDiff", viewMode, operations);
    }

    private static DiffOperation ToOperation(
        DiffResult result,
        string line,
        ref int oldLine,
        ref int newLine)
    {
        if (result.added == true)
        {
            return new DiffOperation("Inserted", null, newLine++, null, line);
        }

        if (result.removed == true)
        {
            return new DiffOperation("Deleted", oldLine++, null, line, null);
        }

        return new DiffOperation("Unchanged", oldLine++, newLine++, line, line);
    }
}
