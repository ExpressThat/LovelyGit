using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed partial class CommitFileDiffCacheRepository
{
    internal static CommitFileDiffCacheEntry CreateCacheEntry(
        string id,
        Guid repositoryId,
        string hash,
        string path,
        CommitFileDiffResponse response,
        bool ignoreWhitespace) => new()
        {
            Id = id,
            RepositoryId = repositoryId,
            Hash = hash,
            Path = path,
            ViewMode = response.ViewMode.ToString(),
            IgnoreWhitespace = ignoreWhitespace,
            Status = response.Status,
            IsBinary = response.IsBinary,
            HasDifferences = response.HasDifferences,
            LineCount = response.Lines.Count,
            IsTruncated = response.IsTruncated,
            TruncationMessage = response.TruncationMessage,
            VirtualText = response.VirtualText ?? string.Empty,
            VirtualTextGzipBase64 = response.VirtualTextGzipBase64 ?? string.Empty,
            VirtualTextEncoding = response.VirtualTextEncoding ?? string.Empty,
            VirtualChangeType = response.VirtualChangeType ?? string.Empty,
            VirtualLineCount = response.VirtualLineCount,
            CompactLineSchema = response.CompactLineSchema ?? string.Empty,
            CompactLinesGzipBase64 = response.CompactLinesGzipBase64 ?? string.Empty,
            CompactLineCount = response.CompactLineCount,
        };

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

    internal static CommitFileDiffResponse ToResponse(
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
            IsTruncated = cache.IsTruncated,
            TruncationMessage = cache.TruncationMessage,
            VirtualText = cache.VirtualText,
            VirtualTextGzipBase64 = cache.VirtualTextGzipBase64,
            VirtualTextEncoding = cache.VirtualTextEncoding,
            VirtualChangeType = cache.VirtualChangeType,
            VirtualLineCount = cache.VirtualLineCount,
            CompactLineSchema = cache.CompactLineSchema,
            CompactLinesGzipBase64 = cache.CompactLinesGzipBase64,
            CompactLineCount = cache.CompactLineCount,
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
