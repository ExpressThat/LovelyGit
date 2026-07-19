using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictResultPayload
{
    internal const int MaximumTextBytes = 4 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static string? Expand(string? text, string compressed)
    {
        if (compressed.Length == 0) return text;
        if (text is not null)
        {
            throw new ArgumentException("Provide one conflict-result encoding.");
        }

        using var source = new MemoryStream(Convert.FromBase64String(compressed));
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            int read;
            while ((read = gzip.Read(buffer, 0, buffer.Length)) != 0)
            {
                if (output.Length + read > MaximumTextBytes)
                {
                    throw new ArgumentException("The conflict result is too large.");
                }
                output.Write(buffer, 0, read);
            }
            return StrictUtf8.GetString(output.GetBuffer(), 0, checked((int)output.Length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
