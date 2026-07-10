using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed partial class CommitFileDiffCacheRepository
{
    private static CommitFileDiffLine Normalize(CommitFileDiffLine line)
    {
        var oldText = Truncate(line.OldText);
        var newText = Truncate(line.NewText);
        var text = Truncate(line.Text);
        return new CommitFileDiffLine
        {
            OldLineNumber = line.OldLineNumber,
            NewLineNumber = line.NewLineNumber,
            OldText = oldText,
            NewText = newText,
            Text = text,
            ChangeType = line.ChangeType,
            OldSyntaxSpans = TrimSpans(line.OldSyntaxSpans, oldText.Length),
            NewSyntaxSpans = TrimSpans(line.NewSyntaxSpans, newText.Length),
            SyntaxSpans = TrimSpans(line.SyntaxSpans, text.Length),
            OldChangeSpans = TrimSpans(line.OldChangeSpans, oldText.Length),
            NewChangeSpans = TrimSpans(line.NewChangeSpans, newText.Length),
            ChangeSpans = TrimSpans(line.ChangeSpans, text.Length),
        };
    }

    private static string Truncate(string? value)
    {
        value ??= string.Empty;
        return value.Length <= MaxCachedLineLength
            ? value
            : string.Concat(
                value.AsSpan(0, MaxCachedLineLength),
                " ... [line truncated]");
    }

    private static List<CommitFileDiffSyntaxSpan> TrimSpans(
        IEnumerable<CommitFileDiffSyntaxSpan>? spans,
        int maxLength)
    {
        var trimmed = new List<CommitFileDiffSyntaxSpan>();
        if (spans == null)
        {
            return trimmed;
        }

        foreach (var span in spans)
        {
            if (span.Start >= maxLength)
            {
                continue;
            }

            var length = Math.Min(span.Length, maxLength - span.Start);
            if (length > 0)
            {
                trimmed.Add(new CommitFileDiffSyntaxSpan
                {
                    Start = span.Start,
                    Length = length,
                    Scope = span.Scope,
                });
            }
        }

        return trimmed;
    }

    private static List<CommitFileDiffChangeSpan> TrimSpans(
        IEnumerable<CommitFileDiffChangeSpan>? spans,
        int maxLength)
    {
        var trimmed = new List<CommitFileDiffChangeSpan>();
        if (spans == null)
        {
            return trimmed;
        }

        foreach (var span in spans)
        {
            if (span.Start >= maxLength)
            {
                continue;
            }

            var length = Math.Min(span.Length, maxLength - span.Start);
            if (length > 0)
            {
                trimmed.Add(new CommitFileDiffChangeSpan
                {
                    Start = span.Start,
                    Length = length,
                    ChangeType = span.ChangeType,
                });
            }
        }

        return trimmed;
    }
}
