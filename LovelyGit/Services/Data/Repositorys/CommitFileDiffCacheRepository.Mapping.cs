using BLite.Core.Query;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using System.Security.Cryptography;
using System.Text;

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

    private static string Truncate(string value)
    {
        if (value.Length <= MaxCachedLineLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, MaxCachedLineLength), " ... [line truncated]");
    }

    private static List<CommitFileDiffSyntaxSpan> TrimSpans(
        IEnumerable<CommitFileDiffSyntaxSpan> spans,
        int maxLength)
    {
        var trimmed = new List<CommitFileDiffSyntaxSpan>();
        foreach (var span in spans)
        {
            if (span.Start >= maxLength)
            {
                continue;
            }

            var length = Math.Min(span.Length, maxLength - span.Start);
            if (length <= 0)
            {
                continue;
            }

            trimmed.Add(new CommitFileDiffSyntaxSpan
            {
                Start = span.Start,
                Length = length,
                Scope = span.Scope,
            });
        }

        return trimmed;
    }

    private static List<CommitFileDiffChangeSpan> TrimSpans(
        IEnumerable<CommitFileDiffChangeSpan> spans,
        int maxLength)
    {
        var trimmed = new List<CommitFileDiffChangeSpan>();
        foreach (var span in spans)
        {
            if (span.Start >= maxLength)
            {
                continue;
            }

            var length = Math.Min(span.Length, maxLength - span.Start);
            if (length <= 0)
            {
                continue;
            }

            trimmed.Add(new CommitFileDiffChangeSpan
            {
                Start = span.Start,
                Length = length,
                ChangeType = span.ChangeType,
            });
        }

        return trimmed;
    }

    private static CommitFileDiffLineCache ToCache(CommitFileDiffLine line)
    {
        return new CommitFileDiffLineCache
        {
            OldLineNumber = line.OldLineNumber,
            NewLineNumber = line.NewLineNumber,
            OldText = line.OldText,
            NewText = line.NewText,
            Text = line.Text,
            ChangeType = line.ChangeType,
            OldSyntaxSpans = ToCache(line.OldSyntaxSpans),
            NewSyntaxSpans = ToCache(line.NewSyntaxSpans),
            SyntaxSpans = ToCache(line.SyntaxSpans),
            OldChangeSpans = ToCache(line.OldChangeSpans),
            NewChangeSpans = ToCache(line.NewChangeSpans),
            ChangeSpans = ToCache(line.ChangeSpans),
        };
    }

    private static CommitFileDiffResponse ToResponse(
        CommitFileDiffCacheEntry cache,
        List<CommitFileDiffLineCacheEntry> lines)
    {
        return new CommitFileDiffResponse
        {
            CommitHash = cache.Hash,
            Path = cache.Path,
            Status = cache.Status,
            ViewMode = Enum.TryParse<CommitDiffViewMode>(cache.ViewMode, out var viewMode)
                ? viewMode
                : CommitDiffViewMode.SideBySide,
            IsBinary = cache.IsBinary,
            HasDifferences = cache.HasDifferences,
            Lines = ToResponseLines(lines),
        };
    }

    private static List<CommitFileDiffLine> ToResponseLines(List<CommitFileDiffLineCacheEntry> lines)
    {
        lines.Sort(static (left, right) => left.LineIndex.CompareTo(right.LineIndex));
        var responseLines = new List<CommitFileDiffLine>(lines.Count);
        foreach (var line in lines)
        {
            responseLines.Add(ToResponse(line.Line));
        }

        return responseLines;
    }

    private static CommitFileDiffLine ToResponse(CommitFileDiffLineCache line)
    {
        return new CommitFileDiffLine
        {
            OldLineNumber = line.OldLineNumber,
            NewLineNumber = line.NewLineNumber,
            OldText = line.OldText,
            NewText = line.NewText,
            Text = line.Text,
            ChangeType = line.ChangeType,
            OldSyntaxSpans = ToResponse(line.OldSyntaxSpans),
            NewSyntaxSpans = ToResponse(line.NewSyntaxSpans),
            SyntaxSpans = ToResponse(line.SyntaxSpans),
            OldChangeSpans = ToResponse(line.OldChangeSpans),
            NewChangeSpans = ToResponse(line.NewChangeSpans),
            ChangeSpans = ToResponse(line.ChangeSpans),
        };
    }

    private static List<CommitFileDiffSyntaxSpanCache> ToCache(IEnumerable<CommitFileDiffSyntaxSpan> spans)
    {
        var cached = new List<CommitFileDiffSyntaxSpanCache>();
        foreach (var span in spans)
        {
            cached.Add(new CommitFileDiffSyntaxSpanCache
            {
                Start = span.Start,
                Length = span.Length,
                Scope = span.Scope,
            });
        }

        return cached;
    }

    private static List<CommitFileDiffSyntaxSpan> ToResponse(IEnumerable<CommitFileDiffSyntaxSpanCache> spans)
    {
        var response = new List<CommitFileDiffSyntaxSpan>();
        foreach (var span in spans)
        {
            response.Add(new CommitFileDiffSyntaxSpan
            {
                Start = span.Start,
                Length = span.Length,
                Scope = span.Scope,
            });
        }

        return response;
    }

    private static List<CommitFileDiffChangeSpanCache> ToCache(IEnumerable<CommitFileDiffChangeSpan> spans)
    {
        var cached = new List<CommitFileDiffChangeSpanCache>();
        foreach (var span in spans)
        {
            cached.Add(new CommitFileDiffChangeSpanCache
            {
                Start = span.Start,
                Length = span.Length,
                ChangeType = span.ChangeType,
            });
        }

        return cached;
    }

    private static List<CommitFileDiffChangeSpan> ToResponse(IEnumerable<CommitFileDiffChangeSpanCache> spans)
    {
        var response = new List<CommitFileDiffChangeSpan>();
        foreach (var span in spans)
        {
            response.Add(new CommitFileDiffChangeSpan
            {
                Start = span.Start,
                Length = span.Length,
                ChangeType = span.ChangeType,
            });
        }

        return response;
    }

}
