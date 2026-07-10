using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed partial class CommitFileDiffCacheRepository
{
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

    private static List<CommitFileDiffSyntaxSpanCache> ToCache(IEnumerable<CommitFileDiffSyntaxSpan>? spans)
    {
        var cached = new List<CommitFileDiffSyntaxSpanCache>();
        if (spans == null)
        {
            return cached;
        }

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

    private static List<CommitFileDiffSyntaxSpan> ToResponse(IEnumerable<CommitFileDiffSyntaxSpanCache>? spans)
    {
        var response = new List<CommitFileDiffSyntaxSpan>();
        if (spans == null)
        {
            return response;
        }

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

    private static List<CommitFileDiffChangeSpanCache> ToCache(IEnumerable<CommitFileDiffChangeSpan>? spans)
    {
        var cached = new List<CommitFileDiffChangeSpanCache>();
        if (spans == null)
        {
            return cached;
        }

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

    private static List<CommitFileDiffChangeSpan> ToResponse(IEnumerable<CommitFileDiffChangeSpanCache>? spans)
    {
        var response = new List<CommitFileDiffChangeSpan>();
        if (spans == null)
        {
            return response;
        }

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
