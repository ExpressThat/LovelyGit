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
            var bytes = await _repository.ReadBlobAsync(file.ObjectId, cancellationToken).ConfigureAwait(false);
            var isBinary = IsBinary(bytes);
            return isBinary
                ? new BlobAnalysis(isBinary, Array.Empty<LineFingerprint>())
                : new BlobAnalysis(isBinary, BuildLineFingerprints(bytes));
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return new BlobAnalysis(IsBinary: true, Array.Empty<LineFingerprint>());
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

    private static bool IsBinary(byte[] bytes)
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
