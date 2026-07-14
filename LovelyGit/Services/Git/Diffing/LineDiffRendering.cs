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

        var prefix = CommonPrefix(oldText, newText);
        var suffix = CommonSuffix(oldText, newText, prefix);
        var oldLength = oldText.Length - prefix - suffix;
        var newLength = newText.Length - prefix - suffix;
        if (oldLength == 0)
            return ([], newLength == 0 ? [] : [Span(prefix, newLength, "Inserted")]);
        if (newLength == 0)
            return ([Span(prefix, oldLength, "Deleted")], []);

        var oldMiddle = oldText.AsSpan(prefix, oldLength).ToArray();
        var newMiddle = newText.AsSpan(prefix, newLength).ToArray();
        var edits = new spkl.Diffs.MyersDiff<char>(oldMiddle, newMiddle).GetEditScript();
        var oldSpans = new List<CommitFileDiffChangeSpan>();
        var newSpans = new List<CommitFileDiffChangeSpan>();
        foreach (var (oldStart, newStart, oldCount, newCount) in edits)
        {
            if (oldCount > 0) oldSpans.Add(Span(prefix + oldStart, oldCount, "Deleted"));
            if (newCount > 0) newSpans.Add(Span(prefix + newStart, newCount, "Inserted"));
        }
        return (oldSpans, newSpans);
    }

    private static int CommonPrefix(string oldText, string newText) =>
        oldText.AsSpan().CommonPrefixLength(newText);

    private static int CommonSuffix(string oldText, string newText, int prefix)
    {
        var length = Math.Min(oldText.Length, newText.Length) - prefix;
        var count = 0;
        while (count < length && oldText[^(count + 1)] == newText[^(count + 1)]) count++;
        return count;
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
