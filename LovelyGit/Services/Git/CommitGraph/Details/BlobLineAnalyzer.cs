using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;

internal sealed partial class BlobLineAnalyzer
{
    private readonly LovelyGitRepository _repository;

    public BlobLineAnalyzer(LovelyGitRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<BlobAnalysis> AnalyzeAsync(
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

    public async ValueTask<BlobLineSummary> SummarizeAsync(
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

    public async ValueTask<BlobText> ReadTextAsync(
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

    private async ValueTask<byte[]> ReadBlobBytesAsync(
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

}

internal readonly record struct LineFingerprint(ulong Hash, int Length);

internal readonly record struct BlobAnalysis(bool IsBinary, LineFingerprint[] Lines);

internal readonly record struct BlobLineSummary(bool IsBinary, int LineCount);

internal sealed record BlobText(bool IsBinary, string Text);
