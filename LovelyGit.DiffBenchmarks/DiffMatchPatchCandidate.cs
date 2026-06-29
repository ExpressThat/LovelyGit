using DiffMatchPatch;

namespace LovelyGit.DiffBenchmarks;

internal static class DiffMatchPatchCandidate
{
    public static CommitFileDiffResponse Run(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        var oldText = ignoreWhitespace ? benchmarkCase.OldText.Trim() : benchmarkCase.OldText;
        var newText = ignoreWhitespace ? benchmarkCase.NewText.Trim() : benchmarkCase.NewText;
        var dmp = new diff_match_patch();
        var diffs = dmp.diff_main(oldText, newText, checklines: true);
        dmp.diff_cleanupSemantic(diffs);
        var operations = new List<DiffOperation>();
        var oldLine = 1;
        var newLine = 1;
        foreach (var diff in diffs)
        {
            foreach (var line in DiffResponseFactory.Lines(diff.text))
            {
                operations.Add(diff.operation switch
                {
                    Operation.INSERT => new DiffOperation("Inserted", null, newLine++, null, line),
                    Operation.DELETE => new DiffOperation("Deleted", oldLine++, null, line, null),
                    _ => new DiffOperation("Unchanged", oldLine++, newLine++, line, line),
                });
            }
        }

        return DiffResponseFactory.FromOperations("DiffMatchPatch", viewMode, operations);
    }
}
