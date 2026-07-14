namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static partial class LineDiffEngine
{
    public static PreparedLineText Prepare(string text) =>
        new(SplitLines(text), EndsWithNewLine(text));

    public static PreparedLineText Prepare(string text, PreparedLineText reuseSameIndexFrom) =>
        new(SplitLines(text, reuseSameIndexFrom.Lines), EndsWithNewLine(text));

    public static string[] SplitLines(string text) => SplitLines(text, []);

    private static string[] SplitLines(string text, string[] reuseSameIndexFrom)
    {
        if (text.Length == 0) return [];
        var separatorCount = CountSeparators(text);
        var endsWithNewLine = EndsWithNewLine(text);
        var lines = new string[separatorCount + (endsWithNewLine ? 0 : 1)];
        var lineStart = 0;
        var lineIndex = 0;
        while (lineStart < text.Length && lineIndex < lines.Length)
        {
            var relativeIndex = text.AsSpan(lineStart).IndexOfAny('\r', '\n');
            if (relativeIndex < 0) break;
            var separatorIndex = lineStart + relativeIndex;
            lines[lineIndex] = CreateOrReuseLine(
                text, lineStart, relativeIndex, lineIndex, reuseSameIndexFrom);
            lineIndex++;
            lineStart = AdvancePastSeparator(text, separatorIndex);
        }
        if (!endsWithNewLine)
            lines[^1] = CreateOrReuseLine(text, lineStart, text.Length - lineStart, lines.Length - 1, reuseSameIndexFrom);
        return lines;
    }

    private static int CountSeparators(string text)
    {
        var count = 0;
        var position = 0;
        while (position < text.Length)
        {
            var relativeIndex = text.AsSpan(position).IndexOfAny('\r', '\n');
            if (relativeIndex < 0) break;
            count++;
            position = AdvancePastSeparator(text, position + relativeIndex);
        }
        return count;
    }

    private static int AdvancePastSeparator(string text, int separatorIndex) =>
        text[separatorIndex] == '\r' && separatorIndex + 1 < text.Length && text[separatorIndex + 1] == '\n'
            ? separatorIndex + 2
            : separatorIndex + 1;

    private static string CreateOrReuseLine(
        string text,
        int start,
        int length,
        int lineIndex,
        string[] reuseSameIndexFrom)
    {
        var candidate = text.AsSpan(start, length);
        if (lineIndex < reuseSameIndexFrom.Length && candidate.SequenceEqual(reuseSameIndexFrom[lineIndex]))
            return reuseSameIndexFrom[lineIndex];
        return new string(candidate);
    }

    private static bool EndsWithNewLine(string text) => text.EndsWith('\n') || text.EndsWith('\r');
}
