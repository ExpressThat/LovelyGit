using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static partial class ConflictTextBundleCodec
{
    internal const int MaximumDecodingBufferBytes = 64 * 1024;

    public static ConflictTexts Expand(string bundle)
    {
        var bytes = Convert.FromBase64String(bundle);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        var encodedRowCount = ReadVarUInt(gzip);
        if (encodedRowCount > MaximumRows) throw InvalidBundle("row count is too large");
        var rowCount = (int)encodedRowCount;
        var sources = new[] { new StringBuilder(), new StringBuilder(), new StringBuilder() };
        var result = new StringBuilder();
        var byteBuffer = ArrayPool<byte>.Shared.Rent(MaximumDecodingBufferBytes);
        var characterBuffer = ArrayPool<char>.Shared.Rent(MaximumDecodingBufferBytes);
        try
        {
            var decoder = StrictUtf8.GetDecoder();
            for (var row = 0; row < rowCount; row++)
            {
                for (var index = 0; index < sources.Length; index++)
                {
                    ReadText(gzip, sources[index], decoder, byteBuffer, characterBuffer);
                }
            }
            var hasResult = ReadText(gzip, result, decoder, byteBuffer, characterBuffer);
            if (gzip.ReadByte() >= 0) throw InvalidBundle("contains trailing data");
            return new(
                sources[0].ToString(),
                sources[1].ToString(),
                sources[2].ToString(),
                hasResult ? result.ToString() : null);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuffer);
            ArrayPool<char>.Shared.Return(characterBuffer);
        }
    }

    private static bool ReadText(
        Stream input,
        StringBuilder target,
        Decoder decoder,
        byte[] byteBuffer,
        char[] characterBuffer)
    {
        var encodedLength = ReadVarUInt(input);
        if (encodedLength == 0) return false;
        if (encodedLength - 1 > MaximumEncodedTextBytes) throw InvalidBundle("text is too large");
        var remaining = (int)encodedLength - 1;
        decoder.Reset();
        try
        {
            while (remaining > 0)
            {
                var count = Math.Min(remaining, MaximumDecodingBufferBytes);
                input.ReadExactly(byteBuffer.AsSpan(0, count));
                decoder.Convert(
                    byteBuffer.AsSpan(0, count),
                    characterBuffer,
                    flush: count == remaining,
                    out var bytesUsed,
                    out var charactersUsed,
                    out _);
                if (bytesUsed != count) throw InvalidBundle("could not decode text");
                target.Append(characterBuffer.AsSpan(0, charactersUsed));
                remaining -= count;
            }
            return true;
        }
        catch (EndOfStreamException)
        {
            throw InvalidBundle("is truncated");
        }
        catch (DecoderFallbackException)
        {
            throw InvalidBundle("contains invalid UTF-8");
        }
    }
}
