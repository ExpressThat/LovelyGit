using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static partial class ConflictTextBundleCodec
{
    private const int SourceCount = 3;
    private const int MaximumRows = 4 * 1024 * 1024;
    private const int MaximumEncodedTextBytes = 256 * 1024 * 1024;
    internal const int MaximumEncodingBufferBytes = 64 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static string Compress(
        string? baseText,
        string? oursText,
        string? theirsText,
        string? resultText,
        CompressionLevel compressionLevel = CompressionLevel.Fastest)
    {
        var sources = new[] { baseText, oursText, theirsText };
        Span<int> positions = stackalloc int[SourceCount];
        var rowCount = 0;
        foreach (var source in sources)
        {
            rowCount = Math.Max(rowCount, CountLines(source));
        }
        var buffer = ArrayPool<byte>.Shared.Rent(MaximumEncodingBufferBytes);
        using var output = new MemoryStream();
        try
        {
            using var gzip = new GZipStream(output, compressionLevel, leaveOpen: true);
            using var buffered = new BufferedStream(gzip, 64 * 1024);
            WriteVarUInt(buffered, checked((uint)rowCount));
            while (HasRemainingText(sources, positions))
            {
                for (var index = 0; index < sources.Length; index++)
                {
                    WriteNextLine(buffered, sources[index], ref positions[index], buffer);
                }
            }
            WriteText(buffered, resultText.AsSpan(), resultText is not null, buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        return Convert.ToBase64String(output.GetBuffer(), 0, checked((int)output.Length));
    }

    private static int CountLines(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var lineCount = 0;
        var lineStart = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\n') continue;
            lineCount++;
            lineStart = index + 1;
        }
        return lineStart < text.Length ? lineCount + 1 : lineCount;
    }

    private static bool HasRemainingText(string?[] texts, ReadOnlySpan<int> positions)
    {
        for (var index = 0; index < texts.Length; index++)
        {
            if (texts[index] is { } text && positions[index] < text.Length) return true;
        }
        return false;
    }

    private static void WriteNextLine(Stream output, string? text, ref int position, byte[] buffer)
    {
        if (text is null || position >= text.Length)
        {
            WriteText(output, [], exists: false, buffer);
            return;
        }
        var remaining = text.AsSpan(position);
        var newline = remaining.IndexOf('\n');
        var length = newline < 0 ? remaining.Length : newline + 1;
        WriteText(output, remaining[..length], exists: true, buffer);
        position += length;
    }

    private static void WriteText(Stream output, ReadOnlySpan<char> text, bool exists, byte[] buffer)
    {
        if (!exists)
        {
            WriteVarUInt(output, 0);
            return;
        }
        var byteCount = StrictUtf8.GetByteCount(text);
        if (byteCount > MaximumEncodedTextBytes) throw InvalidBundle("text is too large");
        WriteVarUInt(output, checked((uint)byteCount + 1));
        if (byteCount == 0) return;
        var encoder = StrictUtf8.GetEncoder();
        while (!text.IsEmpty)
        {
            encoder.Convert(
                text,
                buffer,
                flush: true,
                out var charactersUsed,
                out var bytesUsed,
                out _);
            if (charactersUsed == 0 && bytesUsed == 0)
            {
                throw InvalidBundle("could not encode text");
            }
            output.Write(buffer, 0, bytesUsed);
            text = text[charactersUsed..];
        }
    }

    private static void WriteVarUInt(Stream output, uint value)
    {
        Span<byte> bytes = stackalloc byte[5];
        var count = 0;
        do
        {
            bytes[count] = (byte)(value & 0x7f);
            value >>= 7;
            if (value != 0) bytes[count] |= 0x80;
            count++;
        } while (value != 0);
        output.Write(bytes[..count]);
    }

    private static uint ReadVarUInt(Stream input)
    {
        uint value = 0;
        for (var index = 0; index < 5; index++)
        {
            var next = input.ReadByte();
            if (next < 0) throw InvalidBundle("is truncated");
            if (index == 4 && (next & 0xf0) != 0) throw InvalidBundle("contains an invalid integer");
            value |= (uint)(next & 0x7f) << (index * 7);
            if ((next & 0x80) == 0) return value;
        }
        throw InvalidBundle("contains an invalid integer");
    }

    private static InvalidDataException InvalidBundle(string reason) =>
        new($"The compact conflict text bundle {reason}.");
}
