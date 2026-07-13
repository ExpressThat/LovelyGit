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

    [Fact]
    public void LocalizedEdit_TrimsUnchangedEdgesBeforeRunningMyers()
    {
        var lines = Enumerable.Range(0, 100_000).Select(index => $"line {index} stable").ToArray();
        var changed = (string[])lines.Clone();
        changed[50_000] = "line 50000 changed";
        var oldText = LineDiffEngine.Prepare(string.Join('\n', lines));
        var newText = LineDiffEngine.Prepare(string.Join('\n', changed));
        _ = LineDiffEngine.BuildUnaligned(oldText, newText);

        var localized = Measure(() => LineDiffEngine.BuildUnaligned(oldText, newText));

        output.WriteLine(
            $"Localized 100k-line diff: {localized.Elapsed.TotalMilliseconds:N1} ms, {localized.Allocated:N0} bytes");
        Assert.True(
            localized.Allocated < 100_000,
            $"Localized diff allocated {localized.Allocated:N0} bytes.");
    }

    [Fact]
    public void DistributedEdits_UseStableAnchorsToBoundMyersWork()
    {
        var lines = Enumerable.Range(0, 40_000).Select(index => $"line {index} stable").ToArray();
        var changed = (string[])lines.Clone();
        for (var index = 0; index < changed.Length; index += 10)
            changed[index] = $"line {index} changed";
        var oldText = LineDiffEngine.Prepare(string.Join('\n', lines));
        var newText = LineDiffEngine.Prepare(string.Join('\n', changed));

        var distributed = Measure(() => LineDiffEngine.BuildUnaligned(oldText, newText));

        output.WriteLine(
            $"Distributed 40k-line diff: {distributed.Elapsed.TotalMilliseconds:N1} ms, {distributed.Allocated:N0} bytes");
        Assert.True(
            distributed.Elapsed < TimeSpan.FromMilliseconds(100),
            $"Distributed diff took {distributed.Elapsed.TotalMilliseconds:N1} ms.");
        Assert.True(
            distributed.Allocated < 2_700_000,
            $"Distributed diff allocated {distributed.Allocated:N0} bytes.");
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
