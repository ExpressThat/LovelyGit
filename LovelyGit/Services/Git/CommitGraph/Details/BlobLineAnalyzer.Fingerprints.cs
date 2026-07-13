namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;

internal sealed partial class BlobLineAnalyzer
{
    public static (uint Additions, uint Deletions) CalculateLineStats(
        BlobAnalysis oldBlob,
        BlobAnalysis newBlob)
    {
        if (oldBlob.IsBinary || newBlob.IsBinary)
        {
            return (0, 0);
        }

        var common = CountCommonLines(oldBlob.Lines, newBlob.Lines);
        return ((uint)(newBlob.Lines.Length - common), (uint)(oldBlob.Lines.Length - common));
    }

    private static int CountCommonLines(LineFingerprint[] oldLines, LineFingerprint[] newLines)
    {
        if (oldLines.Length == 0 || newLines.Length == 0)
        {
            return 0;
        }

        if (oldLines.Length <= 16 && newLines.Length <= 16)
        {
            return CountSmallCommonLines(oldLines, newLines);
        }

        var counts = new Dictionary<LineFingerprint, int>(oldLines.Length);
        foreach (var line in oldLines)
        {
            counts.TryGetValue(line, out var count);
            counts[line] = count + 1;
        }

        var common = 0;
        foreach (var line in newLines)
        {
            if (!counts.TryGetValue(line, out var count) || count == 0)
            {
                continue;
            }

            common++;
            if (count == 1)
            {
                counts.Remove(line);
            }
            else
            {
                counts[line] = count - 1;
            }
        }

        return common;
    }

    private static int CountSmallCommonLines(
        ReadOnlySpan<LineFingerprint> oldLines,
        ReadOnlySpan<LineFingerprint> newLines)
    {
        Span<bool> matched = stackalloc bool[newLines.Length];
        var common = 0;
        foreach (var oldLine in oldLines)
        {
            for (var index = 0; index < newLines.Length; index++)
            {
                if (matched[index] || oldLine != newLines[index])
                {
                    continue;
                }

                matched[index] = true;
                common++;
                break;
            }
        }

        return common;
    }

    private static LineFingerprint[] BuildLineFingerprints(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return Array.Empty<LineFingerprint>();
        }

        var lineCount = CountLines(bytes);
        var lines = new LineFingerprint[lineCount];
        var start = 0;
        var lineIndex = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != (byte)'\n')
            {
                continue;
            }

            lines[lineIndex++] = BuildLineFingerprint(bytes.AsSpan(start, index - start));
            start = index + 1;
        }

        if (start < bytes.Length)
        {
            lines[lineIndex] = BuildLineFingerprint(bytes.AsSpan(start));
        }

        return lines;
    }

    private static int CountLines(ReadOnlySpan<byte> bytes)
    {
        var count = 0;
        foreach (var value in bytes)
        {
            if (value == (byte)'\n')
            {
                count++;
            }
        }

        return bytes[^1] == (byte)'\n' ? count : count + 1;
    }

    private static LineFingerprint BuildLineFingerprint(ReadOnlySpan<byte> line)
    {
        if (line.Length > 0 && line[^1] == (byte)'\r')
        {
            line = line[..^1];
        }

        return new LineFingerprint(HashLine(line), line.Length);
    }

    private static ulong HashLine(ReadOnlySpan<byte> line)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        var hash = offset;
        foreach (var value in line)
        {
            hash ^= value;
            hash *= prime;
        }

        return hash;
    }
}
