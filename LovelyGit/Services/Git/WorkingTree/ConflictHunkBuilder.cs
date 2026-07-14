using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictHunkBuilder
{
    public static List<ConflictHunk> Build(
        string baseText,
        string currentText,
        string incomingText,
        string resultText)
    {
        var currentDiff = BuildLineModel(baseText, currentText);
        var incomingDiff = BuildLineModel(baseText, incomingText);
        return Build(resultText, currentDiff, incomingDiff);
    }

    public static List<ConflictHunk> Build(
        string resultText,
        LineDiffModel currentDiff,
        LineDiffModel incomingDiff)
    {
        var parsed = Parse(resultText);
        var currentLines = currentDiff.NewLines;
        var incomingLines = incomingDiff.NewLines;
        var currentCursor = 0;
        var incomingCursor = 0;
        var hunks = new List<ConflictHunk>(parsed.Count);
        foreach (var conflict in parsed)
        {
            var currentStart = Locate(currentLines, conflict.Current, resultText, conflict, currentCursor);
            var incomingStart = Locate(incomingLines, conflict.Incoming, resultText, conflict, incomingCursor);
            var baseRange = ConflictHunkRangeMapper.Union(
                ConflictHunkRangeMapper.Map(currentDiff, currentStart, conflict.Current.Count),
                ConflictHunkRangeMapper.Map(incomingDiff, incomingStart, conflict.Incoming.Count));
            hunks.Add(CreateHunk(conflict, currentStart, incomingStart, baseRange));
            currentCursor = currentStart + conflict.Current.Count;
            incomingCursor = incomingStart + conflict.Incoming.Count;
        }
        return hunks;
    }

    internal static LineDiffModel BuildLineModel(
        string baseText,
        string sourceText,
        bool ignoreWhitespace = false) =>
        LineDiffEngine.Build(baseText, sourceText, ignoreWhitespace);

    internal static PreparedLineText PrepareLineText(string text) => LineDiffEngine.Prepare(text);

    internal static LineDiffModel BuildLineModel(
        PreparedLineText baseText,
        PreparedLineText sourceText,
        bool ignoreWhitespace = false) =>
        LineDiffEngine.Build(baseText, sourceText, ignoreWhitespace);

    private static ConflictHunk CreateHunk(
        ParsedConflict conflict,
        int currentStart,
        int incomingStart,
        ConflictHunkRangeMapper.LineRange baseRange) => new()
    {
        Id = conflict.Id,
        BaseStartLine = baseRange.Start + 1,
        BaseLineCount = baseRange.Count,
        CurrentStartLine = currentStart + 1,
        CurrentLineCount = conflict.Current.Count,
        IncomingStartLine = incomingStart + 1,
        IncomingLineCount = conflict.Incoming.Count,
    };

    internal static IReadOnlyList<string> SplitLines(string text) => LineDiffEngine.SplitLines(text);

    private static List<ParsedConflict> Parse(string text)
    {
        var conflicts = new List<ParsedConflict>();
        var commonStart = 0;
        var searchStart = 0;
        while (searchStart < text.Length)
        {
            var openingStart = FindMarker(text, "<<<<<<<", searchStart);
            if (openingStart < 0) break;
            var parsed = ReadConflict(text, openingStart, commonStart, conflicts.Count);
            if (parsed is null)
            {
                searchStart = LineEnd(text, openingStart);
                continue;
            }
            conflicts.Add(parsed);
            commonStart = parsed.End;
            searchStart = parsed.End;
        }
        return conflicts;
    }

    private static ParsedConflict? ReadConflict(string text, int start, int commonStart, int id)
    {
        var current = new List<string>();
        var incoming = new List<string>();
        List<string>? target = current;
        var foundSeparator = false;
        var cursor = LineEnd(text, start);
        while (cursor < text.Length)
        {
            var end = LineEnd(text, cursor);
            if (text.AsSpan(cursor).StartsWith("|||||||", StringComparison.Ordinal)) target = null;
            else if (text.AsSpan(cursor).StartsWith("=======", StringComparison.Ordinal))
            {
                target = incoming;
                foundSeparator = true;
            }
            else if (text.AsSpan(cursor).StartsWith(">>>>>>>", StringComparison.Ordinal))
            {
                return foundSeparator
                    ? new ParsedConflict(id, commonStart, start, current, incoming, end)
                    : null;
            }
            else if (target is not null) target.Add(ReadLine(text, cursor, end));
            cursor = end;
        }
        return null;
    }

    private static int Locate(
        IReadOnlyList<string> source,
        IReadOnlyList<string> target,
        string resultText,
        ParsedConflict conflict,
        int cursor)
    {
        if (target.Count > 0)
        {
            var found = FindSequence(source, target, cursor);
            return found >= 0 ? found : cursor;
        }

        var commonEnd = conflict.CommonEnd;
        while (commonEnd > conflict.CommonStart)
        {
            if (resultText[commonEnd - 1] == '\n') commonEnd--;
            if (commonEnd > conflict.CommonStart && resultText[commonEnd - 1] == '\r') commonEnd--;
            var commonLineStart = commonEnd;
            while (commonLineStart > conflict.CommonStart &&
                   resultText[commonLineStart - 1] is not ('\r' or '\n')) commonLineStart--;
            var commonLine = resultText.AsSpan(commonLineStart, commonEnd - commonLineStart);
            for (var sourceIndex = source.Count - 1; sourceIndex >= cursor; sourceIndex--)
            {
                if (commonLine.SequenceEqual(source[sourceIndex])) return sourceIndex + 1;
            }
            commonEnd = commonLineStart;
        }

        return cursor;
    }

    private static int FindSequence(
        IReadOnlyList<string> source,
        IReadOnlyList<string> target,
        int start)
    {
        for (var index = start; index <= source.Count - target.Count; index++)
        {
            var matches = true;
            for (var offset = 0; offset < target.Count; offset++)
            {
                if (!string.Equals(source[index + offset], target[offset], StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindMarker(string text, string marker, int start)
    {
        var index = text.IndexOf(marker, start, StringComparison.Ordinal);
        while (index >= 0)
        {
            if (index == 0 || text[index - 1] == '\n') return index;
            index = text.IndexOf(marker, index + 1, StringComparison.Ordinal);
        }
        return -1;
    }

    private static int LineEnd(string text, int start)
    {
        var newline = text.IndexOf('\n', start);
        return newline < 0 ? text.Length : newline + 1;
    }

    private static string ReadLine(string text, int start, int end)
    {
        var contentEnd = end;
        if (contentEnd > start && text[contentEnd - 1] == '\n') contentEnd--;
        if (contentEnd > start && text[contentEnd - 1] == '\r') contentEnd--;
        return text.Substring(start, contentEnd - start);
    }

    private sealed record ParsedConflict(
        int Id,
        int CommonStart,
        int CommonEnd,
        IReadOnlyList<string> Current,
        IReadOnlyList<string> Incoming,
        int End);

}
