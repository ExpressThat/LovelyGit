using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Diffing;

public sealed class LineDiffStreamingPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void UnalignedBuild_AvoidsRetainingOneRowPerSourceLine()
    {
        var oldLines = Enumerable.Range(0, 20_000).Select(index => $"line {index} stable").ToArray();
        var newLines = (string[])oldLines.Clone();
        for (var index = 499; index < newLines.Length; index += 500)
            newLines[index] = $"line {index} changed";
        var oldText = string.Join('\n', oldLines);
        var newText = string.Join('\n', newLines);
        _ = LineDiffEngine.Build(oldText, newText);
        _ = LineDiffEngine.BuildUnaligned(oldText, newText);

        var aligned = Measure(() => LineDiffEngine.Build(oldText, newText));
        var streamed = Measure(() => LineDiffEngine.BuildUnaligned(oldText, newText));

        output.WriteLine($"Aligned: {aligned.Elapsed.TotalMilliseconds:N1} ms, {aligned.Allocated:N0} bytes");
        output.WriteLine($"Streamed: {streamed.Elapsed.TotalMilliseconds:N1} ms, {streamed.Allocated:N0} bytes");
        Assert.True(
            streamed.Allocated + 150_000 < aligned.Allocated,
            $"Streaming allocated {streamed.Allocated:N0} vs {aligned.Allocated:N0} bytes.");
    }

    private static Measurement Measure(Func<LineDiffModel> action)
    {
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        GC.KeepAlive(action());
        return new(Stopwatch.GetElapsedTime(started), GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private readonly record struct Measurement(TimeSpan Elapsed, long Allocated);
}
