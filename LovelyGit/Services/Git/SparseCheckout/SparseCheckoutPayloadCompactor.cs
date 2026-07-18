using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.SparseCheckout;

internal static class SparseCheckoutPayloadCompactor
{
    private const int CompressionThreshold = 64_000;

    public static SparseCheckoutState CompactIfUseful(SparseCheckoutState state)
    {
        if (state.PatternText.Length < CompressionThreshold) return state;

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        using (var writer = new StreamWriter(
                   gzip, new UTF8Encoding(false), bufferSize: 16 * 1024, leaveOpen: true))
        {
            writer.Write(state.PatternText);
        }

        return state with
        {
            PatternText = string.Empty,
            PatternTextGzipBase64 = Convert.ToBase64String(
                output.GetBuffer(), 0, checked((int)output.Length)),
        };
    }

    public static string ExpandRequest(string patternText, string compressed)
    {
        if (compressed.Length == 0) return patternText;
        if (patternText.Length != 0)
        {
            throw new ArgumentException("Provide one sparse-checkout specification encoding.");
        }

        using var source = new MemoryStream(Convert.FromBase64String(compressed));
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8, false);
        var rented = ArrayPool<char>.Shared.Rent(4_096);
        try
        {
            var result = new StringBuilder();
            int read;
            while ((read = reader.Read(rented, 0, rented.Length)) != 0)
            {
                if (result.Length + read > GitSparseCheckoutCommandService.MaximumPatternTextLength)
                {
                    throw new ArgumentException("The sparse-checkout specification is too large.");
                }
                result.Append(rented, 0, read);
            }
            return result.ToString();
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }
}
