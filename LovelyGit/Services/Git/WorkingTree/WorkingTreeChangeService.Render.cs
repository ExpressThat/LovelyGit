using ColorCode;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeChangeService
{
    private static CommitFileDiffResponse BuildSideBySideResponse(
        string commitHash,
        string path,
        string status,
        string oldText,
        string newText,
        ILanguage? language,
        bool ignoreWhitespace) =>
        BuildSideBySideResponse(
            commitHash,
            path,
            status,
            oldText,
            newText,
            language,
            LineDiffEngine.Build(oldText, newText, ignoreWhitespace));

    internal static CommitFileDiffResponse BuildPreparedLineDiffResponse(
        string commitHash,
        string path,
        string status,
        string oldText,
        string newText,
        LineDiffModel model)
    {
        var language = oldText.Length + newText.Length <= MaxSyntaxHighlightedCharacters
            ? ResolveLanguage(path)
            : null;
        var direct = ConflictComparisonPayloadBuilder.BuildDirectIfUseful(
            commitHash,
            path,
            status,
            model,
            hasSyntaxHighlighting: language is not null);
        if (direct is not null) return direct;
        return BuildSideBySideResponse(commitHash, path, status, oldText, newText, language, model);
    }

    private static CommitFileDiffResponse BuildSideBySideResponse(
        string commitHash,
        string path,
        string status,
        string oldText,
        string newText,
        ILanguage? language,
        LineDiffModel model)
    {
        var syntax = SyntaxSpanBuilder.Create(
            language,
            oldText.Length + newText.Length,
            MaxSyntaxHighlightedCharacters,
            MaxSyntaxHighlightedLineLength);
        var lines = new List<CommitFileDiffLine>(model.Rows.Count);
        foreach (var row in model.Rows)
        {
            var oldLine = row.OldIndex is { } oldIndex ? model.OldLines[oldIndex] : string.Empty;
            var newLine = row.NewIndex is { } newIndex ? model.NewLines[newIndex] : string.Empty;
            var spans = LineDiffRendering.ChangeSpans(oldLine, newLine, row);
            lines.Add(new CommitFileDiffLine
            {
                OldLineNumber = row.OldIndex + 1,
                NewLineNumber = row.NewIndex + 1,
                OldText = oldLine,
                NewText = newLine,
                ChangeType = LineDiffRendering.ChangeType(row),
                OldSyntaxSpans = BuildSyntaxSpans(oldLine, syntax),
                NewSyntaxSpans = BuildSyntaxSpans(newLine, syntax),
                OldChangeSpans = spans.Old,
                NewChangeSpans = spans.New,
            });
        }
        return Response(commitHash, path, status, CommitDiffViewMode.SideBySide, model.HasDifferences, lines);
    }

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
        var syntax = SyntaxSpanBuilder.Create(
            language,
            oldText.Length + newText.Length,
            MaxSyntaxHighlightedCharacters,
            MaxSyntaxHighlightedLineLength);
        var lines = new List<CommitFileDiffLine>(model.Rows.Count + model.Blocks.Count);
        foreach (var row in model.Rows)
        {
            if (!row.IsChanged)
            {
                var text = model.OldLines[row.OldIndex!.Value];
                lines.Add(CombinedLine(row.OldIndex + 1, row.NewIndex + 1, text, "Unchanged", syntax, []));
                continue;
            }
            var oldLine = row.OldIndex is { } oldIndex ? model.OldLines[oldIndex] : string.Empty;
            var newLine = row.NewIndex is { } newIndex ? model.NewLines[newIndex] : string.Empty;
            var spans = LineDiffRendering.ChangeSpans(oldLine, newLine, row);
            if (row.OldIndex is not null)
                lines.Add(CombinedLine(row.OldIndex + 1, null, oldLine, "Deleted", syntax, spans.Old));
            if (row.NewIndex is not null)
                lines.Add(CombinedLine(null, row.NewIndex + 1, newLine, "Inserted", syntax, spans.New));
        }
        return Response(commitHash, path, status, CommitDiffViewMode.Combined, model.HasDifferences, lines);
    }

    private static CommitFileDiffLine CombinedLine(
        int? oldLine,
        int? newLine,
        string text,
        string type,
        SyntaxSpanBuilder syntax,
        List<CommitFileDiffChangeSpan> spans) => new()
    {
        OldLineNumber = oldLine,
        NewLineNumber = newLine,
        Text = text,
        ChangeType = type,
        SyntaxSpans = BuildSyntaxSpans(text, syntax),
        ChangeSpans = spans,
    };

    private static CommitFileDiffResponse Response(
        string hash,
        string path,
        string status,
        CommitDiffViewMode mode,
        bool hasDifferences,
        List<CommitFileDiffLine> lines) => new()
    {
        CommitHash = hash,
        Path = path,
        Status = status,
        ViewMode = mode,
        HasDifferences = hasDifferences,
        Lines = lines,
    };

    private static CommitFileDiffResponse BuildUnreadableFileDiff(
        string hash,
        string path,
        string status,
        CommitDiffViewMode mode) => new()
    {
        CommitHash = hash,
        Path = path,
        Status = status,
        ViewMode = mode,
        IsBinary = true,
        HasDifferences = true,
    };
}
