using ColorCode;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    private static CommitFileDiffResponse BuildCombinedResponse(
        string commitHash,
        string path,
        string status,
        string oldText,
        string newText,
        ILanguage? language,
        bool ignoreWhitespace)
    {
        var model = LineDiffEngine.Build(oldText, newText, ignoreWhitespace);
        var syntaxSpanBuilder = SyntaxSpanBuilder.Create(
            language,
            oldText.Length + newText.Length,
            MaxSyntaxHighlightedCharacters,
            MaxSyntaxHighlightedLineLength);
        var lines = new List<CommitFileDiffLine>(model.Rows.Count * 2);

        foreach (var row in model.Rows)
        {
            AddCombinedLines(lines, model, row, syntaxSpanBuilder);
        }

        return new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = CommitDiffViewMode.Combined,
            IsBinary = false,
            HasDifferences = model.HasDifferences,
            Lines = lines,
        };
    }

    private static void AddCombinedLines(
        List<CommitFileDiffLine> lines,
        LineDiffModel model,
        LineDiffRow row,
        SyntaxSpanBuilder syntax)
    {
        if (!row.IsChanged && row.OldIndex is int oldIndex && row.NewIndex is int newIndex)
        {
            AddCombinedLine(lines, oldIndex + 1, newIndex + 1, model.OldLines[oldIndex], "Unchanged", [], syntax);
            return;
        }

        var oldText = row.OldIndex is int old ? model.OldLines[old] : string.Empty;
        var newText = row.NewIndex is int next ? model.NewLines[next] : string.Empty;
        var spans = LineDiffRendering.ChangeSpans(oldText, newText, row);
        if (row.OldIndex is int deleted)
            AddCombinedLine(lines, deleted + 1, null, oldText, "Deleted", spans.Old, syntax);
        if (row.NewIndex is int inserted)
            AddCombinedLine(lines, null, inserted + 1, newText, "Inserted", spans.New, syntax);
    }

    private static void AddCombinedLine(
        List<CommitFileDiffLine> lines,
        int? oldLine,
        int? newLine,
        string text,
        string type,
        List<CommitFileDiffChangeSpan> spans,
        SyntaxSpanBuilder syntax) => lines.Add(new()
        {
            OldLineNumber = oldLine,
            NewLineNumber = newLine,
            Text = text,
            ChangeType = type,
            SyntaxSpans = BuildSyntaxSpans(text, syntax),
            ChangeSpans = spans,
        });
}
