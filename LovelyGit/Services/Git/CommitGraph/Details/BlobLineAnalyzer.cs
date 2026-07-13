using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;

internal sealed class BlobLineAnalyzer
{
    private readonly LovelyGitRepository _repository;

    public BlobLineAnalyzer(LovelyGitRepository repository)
    {
        _repository = repository;
    }

    public async Task<BlobAnalysis> AnalyzeAsync(
        GitTreeFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var blob = await ReadBlobBytesAsync(file, cancellationToken).ConfigureAwait(false);
            var isBinary = IsBinary(blob);
            return isBinary
                ? new BlobAnalysis(isBinary, Array.Empty<LineFingerprint>())
                : new BlobAnalysis(isBinary, BuildLineFingerprints(blob));
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return new BlobAnalysis(IsBinary: true, Array.Empty<LineFingerprint>());
        }
    }

    public async Task<BlobLineSummary> SummarizeAsync(
        GitTreeFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var blob = await ReadBlobBytesAsync(file, cancellationToken).ConfigureAwait(false);
            return Summarize(blob);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return new BlobLineSummary(IsBinary: true, LineCount: 0);
        }
    }

    public async Task<BlobText> ReadTextAsync(
        GitTreeFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var blob = await ReadBlobBytesAsync(file, cancellationToken).ConfigureAwait(false);
            return IsBinary(blob)
                ? new BlobText(true, string.Empty)
                : new BlobText(false, DecodeText(blob));
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return new BlobText(IsBinary: true, Text: string.Empty);
        }
    }

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

        var counts = new Dictionary<LineFingerprint, int>();
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

    private static bool IsBinary(ReadOnlySpan<byte> bytes)
    {
        var length = Math.Min(bytes.Length, 8000);
        for (var i = 0; i < length; i++)
        {
            if (bytes[i] == 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static BlobLineSummary Summarize(ReadOnlySpan<byte> bytes)
    {
        if (IsBinary(bytes))
        {
            return new BlobLineSummary(IsBinary: true, LineCount: 0);
        }

        var lineCount = 0;
        foreach (var value in bytes)
        {
            if (value == (byte)'\n')
            {
                lineCount++;
            }
        }

        if (bytes.Length > 0 && bytes[^1] != (byte)'\n')
        {
            lineCount++;
        }

        return new BlobLineSummary(IsBinary: false, LineCount: lineCount);
    }

    private static LineFingerprint[] BuildLineFingerprints(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return Array.Empty<LineFingerprint>();
        }

        var lines = new List<LineFingerprint>();
        var start = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != (byte)'\n')
            {
                continue;
            }

            AddLineFingerprint(bytes.AsSpan(start, index - start), lines);
            start = index + 1;
        }

        if (start < bytes.Length)
        {
            AddLineFingerprint(bytes.AsSpan(start), lines);
        }

        return lines.ToArray();
    }

    private async Task<byte[]> ReadBlobBytesAsync(
        GitTreeFile file,
        CancellationToken cancellationToken)
    {
        return await _repository
            .ReadBlobWithoutCachingAsync(file.ObjectId, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string DecodeText(byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static void AddLineFingerprint(ReadOnlySpan<byte> line, List<LineFingerprint> lines)
    {
        if (line.Length > 0 && line[^1] == (byte)'\r')
        {
            line = line[..^1];
        }

        lines.Add(new LineFingerprint(HashLine(line), line.Length));
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

internal readonly record struct LineFingerprint(ulong Hash, int Length);

internal sealed record BlobAnalysis(bool IsBinary, LineFingerprint[] Lines);

internal readonly record struct BlobLineSummary(bool IsBinary, int LineCount);

internal sealed record BlobText(bool IsBinary, string Text);
