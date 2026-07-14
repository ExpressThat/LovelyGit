using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Diffing;

[Collection(PerformanceTestCollection.Name)]
public sealed class LineDiffRenderingPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void LocalizedEdit_DiffsOnlyTheChangedMiddle()
    {
        var oldText = new string('a', 100_000) + "old" + new string('z', 100_000);
        var newText = new string('a', 100_000) + "new" + new string('z', 100_000);
        var row = new LineDiffRow(0, 0, isChanged: true);
        _ = FullLineDiff(oldText, newText);
        _ = LineDiffRendering.ChangeSpans(oldText, newText, row);

        var full = BestOfThree(() => FullLineDiff(oldText, newText));
        var trimmed = BestOfThree(() => LineDiffRendering.ChangeSpans(oldText, newText, row));

        output.WriteLine($"Full: {full.Elapsed.TotalMilliseconds:N2} ms, {full.Allocated:N0} bytes");
        output.WriteLine($"Trimmed: {trimmed.Elapsed.TotalMilliseconds:N2} ms, {trimmed.Allocated:N0} bytes");
        Assert.True(
            trimmed.Elapsed < full.Elapsed * 0.75,
            $"Trimmed diff took {trimmed.Elapsed.TotalMilliseconds:N2} vs {full.Elapsed.TotalMilliseconds:N2} ms.");
        Assert.True(
            trimmed.Allocated < full.Allocated * 0.05,
            $"Trimmed diff allocated {trimmed.Allocated:N0} vs {full.Allocated:N0} bytes.");
    }

    private static object FullLineDiff(string oldText, string newText) =>
        new spkl.Diffs.MyersDiff<char>(oldText.ToCharArray(), newText.ToCharArray()).GetEditScript();

    private static Measurement BestOfThree<T>(Func<T> action)
    {
        var best = Measure(action);
        for (var index = 1; index < 3; index++)
        {
            var candidate = Measure(action);
            if (candidate.Elapsed < best.Elapsed) best = candidate;
        }
        return best;
    }

    private static Measurement Measure<T>(Func<T> action)
    {
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        GC.KeepAlive(action());
        return new(Stopwatch.GetElapsedTime(started), GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private readonly record struct Measurement(TimeSpan Elapsed, long Allocated);
}
