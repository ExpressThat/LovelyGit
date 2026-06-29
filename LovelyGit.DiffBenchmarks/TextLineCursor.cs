namespace LovelyGit.DiffBenchmarks;

internal ref struct TextLineCursor
{
    private ReadOnlySpan<char> remaining;

    public TextLineCursor(string text)
    {
        remaining = text.AsSpan();
    }

    public bool TryRead(out ReadOnlySpan<char> line)
    {
        if (remaining.Length == 0)
        {
            line = default;
            return false;
        }

        var newline = remaining.IndexOf('\n');
        if (newline < 0)
        {
            line = TrimCarriageReturn(remaining);
            remaining = [];
            return true;
        }

        line = TrimCarriageReturn(remaining[..newline]);
        remaining = remaining[(newline + 1)..];
        return true;
    }

    public readonly bool Peek(out ReadOnlySpan<char> line)
    {
        var cursor = this;
        return cursor.TryRead(out line);
    }

    private static ReadOnlySpan<char> TrimCarriageReturn(ReadOnlySpan<char> line)
    {
        return line.Length > 0 && line[^1] == '\r' ? line[..^1] : line;
    }
}
