using System.Diagnostics;
using System.IO.Compression;
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
            binary.Allocated < legacy.Allocated * 0.80,
            $"Binary allocated {binary.Allocated:N0} vs JSON {legacy.Allocated:N0} bytes.");
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
