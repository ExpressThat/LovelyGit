using NGit.Diff;
using System.Text;

namespace LovelyGit.DiffBenchmarks;

internal static class NGitDiffCandidate
{
    public static CommitFileDiffResponse RunMyers(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        return Run("NGitDiff Myers", DiffAlgorithm.SupportedAlgorithm.MYERS, benchmarkCase, viewMode, ignoreWhitespace);
    }

    public static CommitFileDiffResponse RunHistogram(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        return Run("NGitDiff Histogram", DiffAlgorithm.SupportedAlgorithm.HISTOGRAM, benchmarkCase, viewMode, ignoreWhitespace);
    }

    private static CommitFileDiffResponse Run(
        string name,
        DiffAlgorithm.SupportedAlgorithm algorithm,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        var oldText = new RawText(Encoding.UTF8.GetBytes(benchmarkCase.OldText));
        var newText = new RawText(Encoding.UTF8.GetBytes(benchmarkCase.NewText));
        var comparator = ignoreWhitespace ? RawTextComparator.WS_IGNORE_ALL : RawTextComparator.DEFAULT;
        var edits = DiffAlgorithm.GetAlgorithm(algorithm).Diff(comparator, oldText, newText);
        var operations = new List<DiffOperation>();
        var oldIndex = 0;
        var newIndex = 0;
        foreach (Edit edit in edits)
        {
            AddUnchanged(operations, oldText, newText, ref oldIndex, ref newIndex, edit.GetBeginA(), edit.GetBeginB());
            AddChanged(operations, oldText, newText, ref oldIndex, ref newIndex, edit);
        }

        AddUnchanged(operations, oldText, newText, ref oldIndex, ref newIndex, oldText.Size(), newText.Size());
        return DiffResponseFactory.FromOperations(name, viewMode, operations);
    }

    private static void AddUnchanged(
        List<DiffOperation> operations,
        RawText oldText,
        RawText newText,
        ref int oldIndex,
        ref int newIndex,
        int oldEnd,
        int newEnd)
    {
        while (oldIndex < oldEnd && newIndex < newEnd)
        {
            operations.Add(new DiffOperation(
                "Unchanged",
                oldIndex + 1,
                newIndex + 1,
                oldText.GetString(oldIndex),
                newText.GetString(newIndex)));
            oldIndex++;
            newIndex++;
        }
    }

    private static void AddChanged(
        List<DiffOperation> operations,
        RawText oldText,
        RawText newText,
        ref int oldIndex,
        ref int newIndex,
        Edit edit)
    {
        while (oldIndex < edit.GetEndA())
        {
            operations.Add(new DiffOperation("Deleted", oldIndex + 1, null, oldText.GetString(oldIndex), null));
            oldIndex++;
        }

        while (newIndex < edit.GetEndB())
        {
            operations.Add(new DiffOperation("Inserted", null, newIndex + 1, null, newText.GetString(newIndex)));
            newIndex++;
        }
    }
}
