using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class LineDiffRendering
{
    public static string ChangeType(LineDiffRow row) =>
        row.OldIndex is null
            ? "Inserted"
            : row.NewIndex is null
                ? "Deleted"
                : row.IsChanged ? "Modified" : "Unchanged";

    public static (List<CommitFileDiffChangeSpan> Old, List<CommitFileDiffChangeSpan> New) ChangeSpans(
        string oldText,
        string newText,
        LineDiffRow row)
    {
        if (!row.IsChanged) return ([], []);
        if (row.OldIndex is null) return ([], FullSpan(newText, "Inserted"));
        if (row.NewIndex is null) return (FullSpan(oldText, "Deleted"), []);

        var edits = new spkl.Diffs.MyersDiff<char>(oldText.ToCharArray(), newText.ToCharArray())
            .GetEditScript();
        var oldSpans = new List<CommitFileDiffChangeSpan>();
        var newSpans = new List<CommitFileDiffChangeSpan>();
        foreach (var (oldStart, newStart, oldCount, newCount) in edits)
        {
            if (oldCount > 0) oldSpans.Add(Span(oldStart, oldCount, "Deleted"));
            if (newCount > 0) newSpans.Add(Span(newStart, newCount, "Inserted"));
        }
        return (oldSpans, newSpans);
    }

    private static List<CommitFileDiffChangeSpan> FullSpan(string text, string type) =>
        text.Length == 0 ? [] : [Span(0, text.Length, type)];

    private static CommitFileDiffChangeSpan Span(int start, int length, string type) => new()
    {
        Start = start,
        Length = length,
        ChangeType = type,
    };
}
