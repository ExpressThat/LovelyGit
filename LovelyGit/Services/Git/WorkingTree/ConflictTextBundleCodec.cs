using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class ConflictTextBundleCodec
{
    private const int SourceCount = 3;
    private const int MaximumRows = 4 * 1024 * 1024;
    private const int MaximumEncodedTextBytes = 256 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static string Compress(string? baseText, string? oursText, string? theirsText, string? resultText)
    {
        var sources = new[] { baseText, oursText, theirsText };
        Span<int> positions = stackalloc int[SourceCount];
        var rowCount = 0;
        var maximumLineCharacters = 0;
        foreach (var source in sources)
        {
            var shape = AnalyzeSource(source);
            rowCount = Math.Max(rowCount, shape.LineCount);
            maximumLineCharacters = Math.Max(maximumLineCharacters, shape.MaximumLineCharacters);
        }
        var resultByteCount = resultText is null ? 0 : StrictUtf8.GetByteCount(resultText);
        var lineBufferSize = maximumLineCharacters == 0
            ? 0
            : StrictUtf8.GetMaxByteCount(maximumLineCharacters);
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(1, Math.Max(lineBufferSize, resultByteCount)));
        using var output = new MemoryStream();
        try
        {
            using var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true);
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

    public static ConflictTexts Expand(string bundle)
    {
        var bytes = Convert.FromBase64String(bundle);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        var encodedRowCount = ReadVarUInt(gzip);
        if (encodedRowCount > MaximumRows) throw InvalidBundle("row count is too large");
        var rowCount = (int)encodedRowCount;
        var sources = new[] { new StringBuilder(), new StringBuilder(), new StringBuilder() };
        for (var row = 0; row < rowCount; row++)
        {
            for (var index = 0; index < sources.Length; index++)
            {
                if (ReadText(gzip) is { } line) sources[index].Append(line);
            }
        }
        var result = ReadText(gzip);
        if (gzip.ReadByte() >= 0) throw InvalidBundle("contains trailing data");
        return new(sources[0].ToString(), sources[1].ToString(), sources[2].ToString(), result);
    }

    private static SourceShape AnalyzeSource(string? text)
    {
        if (string.IsNullOrEmpty(text)) return default;
        var lineCount = 0;
        var maximumLineCharacters = 0;
        var lineStart = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\n') continue;
            lineCount++;
            maximumLineCharacters = Math.Max(maximumLineCharacters, index - lineStart + 1);
            lineStart = index + 1;
        }
        if (lineStart < text.Length)
        {
            lineCount++;
            maximumLineCharacters = Math.Max(maximumLineCharacters, text.Length - lineStart);
        }
        return new(lineCount, maximumLineCharacters);
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
        var written = StrictUtf8.GetBytes(text, buffer);
        output.Write(buffer, 0, written);
    }

    private static string? ReadText(Stream input)
    {
        var encodedLength = ReadVarUInt(input);
        if (encodedLength == 0) return null;
        if (encodedLength - 1 > MaximumEncodedTextBytes) throw InvalidBundle("text is too large");
        var length = (int)encodedLength - 1;
        if (length == 0) return string.Empty;
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            input.ReadExactly(buffer.AsSpan(0, length));
            return StrictUtf8.GetString(buffer, 0, length);
        }
        catch (EndOfStreamException)
        {
            throw InvalidBundle("is truncated");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
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

    private readonly record struct SourceShape(int LineCount, int MaximumLineCharacters);
}
