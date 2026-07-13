using System.Diagnostics;
using System.Buffers;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictTextPayloadPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void BinaryBundle_AvoidsJsonCompactionAllocations()
    {
        var common = string.Join('\n', Enumerable.Range(0, 20_000).Select(index => $"line {index}")) + '\n';
        var current = common.Replace("line 10000", "current", StringComparison.Ordinal);
        var incoming = common.Replace("line 10000", "incoming", StringComparison.Ordinal);
        _ = ConflictTextBundleCodec.Compress(common, current, incoming, common);
        _ = LegacyCompress(common, current, incoming, common);

        var binary = Measure(() => ConflictTextBundleCodec.Compress(common, current, incoming, common));
        var legacy = Measure(() => LegacyCompress(common, current, incoming, common));

        output.WriteLine($"Binary: {binary.Allocated:N0} bytes, {binary.Elapsed.TotalMilliseconds:N1} ms");
        output.WriteLine($"JSON: {legacy.Allocated:N0} bytes, {legacy.Elapsed.TotalMilliseconds:N1} ms");
        Assert.True(
            binary.Allocated < legacy.Allocated * 0.40,
            $"Binary allocated {binary.Allocated:N0} vs JSON {legacy.Allocated:N0} bytes.");
    }

    [Fact]
    public void BinaryBundle_ExpansionAvoidsPerLineStrings()
    {
        var common = string.Join('\n', Enumerable.Range(0, 20_000).Select(index => $"line {index}")) + '\n';
        var bundle = ConflictTextBundleCodec.Compress(common, common, common, common);
        _ = ConflictTextBundleCodec.Expand(bundle);
        _ = LegacyExpand(bundle);

        var streamed = Measure(() => ConflictTextBundleCodec.Expand(bundle));
        var legacy = Measure(() => LegacyExpand(bundle));

        output.WriteLine($"Streamed expand: {streamed.Allocated:N0} bytes, {streamed.Elapsed.TotalMilliseconds:N1} ms");
        output.WriteLine($"Per-line expand: {legacy.Allocated:N0} bytes, {legacy.Elapsed.TotalMilliseconds:N1} ms");
        Assert.True(
            streamed.Allocated < legacy.Allocated * 0.80,
            $"Streamed expansion allocated {streamed.Allocated:N0} vs {legacy.Allocated:N0} bytes.");
    }

    private static string LegacyCompress(params string[] texts)
    {
        var positions = new int[3];
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        using (var writer = new Utf8JsonWriter(gzip))
        {
            writer.WriteStartArray();
            writer.WriteStartArray();
            while (HasRemaining(texts, positions))
            {
                writer.WriteStartArray();
                for (var index = 0; index < positions.Length; index++)
                {
                    var remaining = texts[index].AsSpan(positions[index]);
                    var newline = remaining.IndexOf('\n');
                    var length = newline < 0 ? remaining.Length : newline + 1;
                    writer.WriteStringValue(remaining[..length]);
                    positions[index] += length;
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteStringValue(texts[3]);
            writer.WriteEndArray();
        }
        return Convert.ToBase64String(output.GetBuffer(), 0, checked((int)output.Length));
    }

    private static ConflictTexts LegacyExpand(string bundle)
    {
        var bytes = Convert.FromBase64String(bundle);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        var rowCount = checked((int)ReadVarUInt(gzip));
        var sources = new[] { new StringBuilder(), new StringBuilder(), new StringBuilder() };
        for (var row = 0; row < rowCount; row++)
        {
            foreach (var source in sources)
            {
                if (ReadText(gzip) is { } line) source.Append(line);
            }
        }
        return new(
            sources[0].ToString(),
            sources[1].ToString(),
            sources[2].ToString(),
            ReadText(gzip));
    }

    private static string? ReadText(Stream input)
    {
        var encodedLength = ReadVarUInt(input);
        if (encodedLength == 0) return null;
        var length = checked((int)encodedLength - 1);
        if (length == 0) return string.Empty;
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            input.ReadExactly(buffer.AsSpan(0, length));
            return Encoding.UTF8.GetString(buffer, 0, length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static uint ReadVarUInt(Stream input)
    {
        uint value = 0;
        for (var index = 0; index < 5; index++)
        {
            var next = input.ReadByte();
            value |= (uint)(next & 0x7f) << (index * 7);
            if ((next & 0x80) == 0) return value;
        }
        throw new InvalidDataException();
    }

    private static bool HasRemaining(string[] texts, int[] positions)
    {
        for (var index = 0; index < positions.Length; index++)
        {
            if (positions[index] < texts[index].Length) return true;
        }
        return false;
    }

    private static Measurement Measure(Func<object> action)
    {
        GC.Collect();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var startedAt = Stopwatch.GetTimestamp();
        GC.KeepAlive(action());
        return new(Stopwatch.GetElapsedTime(startedAt), GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
    }

    private readonly record struct Measurement(TimeSpan Elapsed, long Allocated);
}
