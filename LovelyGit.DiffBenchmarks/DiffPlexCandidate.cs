using DiffPlex;
using DiffPlex.DiffBuilder;

namespace LovelyGit.DiffBenchmarks;

internal static class DiffPlexCandidate
{
    public static CommitFileDiffResponse Run(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        return viewMode == CommitDiffViewMode.SideBySide
            ? SideBySide(benchmarkCase, ignoreWhitespace)
            : Combined(benchmarkCase, ignoreWhitespace);
    }

    private static CommitFileDiffResponse SideBySide(BenchmarkCase benchmarkCase, bool ignoreWhitespace)
    {
        var model = new SideBySideDiffBuilder(new Differ()).BuildDiffModel(
            benchmarkCase.OldText,
            benchmarkCase.NewText,
            ignoreWhitespace);
        var operations = new List<DiffOperation>(Math.Max(model.OldText.Lines.Count, model.NewText.Lines.Count));
        for (var index = 0; index < Math.Max(model.OldText.Lines.Count, model.NewText.Lines.Count); index++)
        {
            var oldLine = index < model.OldText.Lines.Count ? model.OldText.Lines[index] : null;
            var newLine = index < model.NewText.Lines.Count ? model.NewText.Lines[index] : null;
            operations.Add(new DiffOperation(
                DiffPlexLineType(oldLine?.Type, newLine?.Type),
                oldLine?.Position,
                newLine?.Position,
                oldLine?.Text,
                newLine?.Text));
        }

        return DiffResponseFactory.FromOperations("DiffPlex", CommitDiffViewMode.SideBySide, operations);
    }

    private static CommitFileDiffResponse Combined(BenchmarkCase benchmarkCase, bool ignoreWhitespace)
    {
        var model = new InlineDiffBuilder(new Differ()).BuildDiffModel(
            benchmarkCase.OldText,
            benchmarkCase.NewText,
            ignoreWhitespace);
        var oldLine = 1;
        var newLine = 1;
        var operations = new List<DiffOperation>(model.Lines.Count);
        foreach (var line in model.Lines)
        {
            var changeType = DiffResponseFactory.FromDiffPlex(line.Type);
            operations.Add(new DiffOperation(
                changeType,
                line.Type == DiffPlex.DiffBuilder.Model.ChangeType.Inserted ? null : oldLine++,
                line.Type == DiffPlex.DiffBuilder.Model.ChangeType.Deleted ? null : newLine++,
                line.Type == DiffPlex.DiffBuilder.Model.ChangeType.Inserted ? null : line.Text,
                line.Type == DiffPlex.DiffBuilder.Model.ChangeType.Deleted ? null : line.Text));
        }

        return DiffResponseFactory.FromOperations("DiffPlex", CommitDiffViewMode.Combined, operations);
    }

    private static string DiffPlexLineType(
        DiffPlex.DiffBuilder.Model.ChangeType? oldType,
        DiffPlex.DiffBuilder.Model.ChangeType? newType)
    {
        if (oldType == DiffPlex.DiffBuilder.Model.ChangeType.Deleted)
        {
            return newType == DiffPlex.DiffBuilder.Model.ChangeType.Inserted ? "Modified" : "Deleted";
        }

        return newType == DiffPlex.DiffBuilder.Model.ChangeType.Inserted ? "Inserted" : "Unchanged";
    }
}
