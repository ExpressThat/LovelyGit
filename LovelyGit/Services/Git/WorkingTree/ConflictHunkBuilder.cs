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
            var currentStart = Locate(currentLines, conflict.Current, conflict.PreviousCommon, currentCursor);
            var incomingStart = Locate(incomingLines, conflict.Incoming, conflict.PreviousCommon, incomingCursor);
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
        var lines = SplitLines(text);
        var conflicts = new List<ParsedConflict>();
        var common = new List<string>();
        for (var index = 0; index < lines.Count; index++)
        {
            if (!lines[index].StartsWith("<<<<<<<", StringComparison.Ordinal))
            {
                common.Add(lines[index]);
                continue;
            }

            var current = new List<string>();
            var incoming = new List<string>();
            var target = current;
            var foundSeparator = false;
            var end = index + 1;
            for (; end < lines.Count; end++)
            {
                var line = lines[end];
                if (line.StartsWith("|||||||", StringComparison.Ordinal))
                {
                    target = new List<string>();
                }
                else if (line.StartsWith("=======", StringComparison.Ordinal))
                {
                    target = incoming;
                    foundSeparator = true;
                }
                else if (line.StartsWith(">>>>>>>", StringComparison.Ordinal))
                {
                    break;
                }
                else
                {
                    target.Add(line);
                }
            }

            if (!foundSeparator || end >= lines.Count)
            {
                common.Add(lines[index]);
                continue;
            }

            conflicts.Add(new ParsedConflict(conflicts.Count, common.ToArray(), current, incoming));
            common.Clear();
            index = end;
        }

        return conflicts;
    }

    private static int Locate(
        IReadOnlyList<string> source,
        IReadOnlyList<string> target,
        IReadOnlyList<string> previousCommon,
        int cursor)
    {
        if (target.Count > 0)
        {
            var found = FindSequence(source, target, cursor);
            return found >= 0 ? found : cursor;
        }

        for (var commonIndex = previousCommon.Count - 1; commonIndex >= 0; commonIndex--)
        {
            for (var sourceIndex = source.Count - 1; sourceIndex >= cursor; sourceIndex--)
            {
                if (string.Equals(previousCommon[commonIndex], source[sourceIndex], StringComparison.Ordinal))
                {
                    return sourceIndex + 1;
                }
            }
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

    private sealed record ParsedConflict(
        int Id,
        IReadOnlyList<string> PreviousCommon,
        IReadOnlyList<string> Current,
        IReadOnlyList<string> Incoming);

}
