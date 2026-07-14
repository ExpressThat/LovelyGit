using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Diffing;

[Collection(PerformanceTestCollection.Name)]
public sealed class LineDiffPreparationTests(ITestOutputHelper output)
{
    [Fact]
    public void Prepare_WithBaseline_ReusesOnlyEqualLinesAtMatchingIndices()
    {
        var baseline = LineDiffEngine.Prepare("same\r\nbase\nlast");

        var prepared = LineDiffEngine.Prepare("same\nchanged\r\nlast\r\n", baseline);

        Assert.Same(baseline.Lines[0], prepared.Lines[0]);
        Assert.NotSame(baseline.Lines[1], prepared.Lines[1]);
        Assert.Same(baseline.Lines[2], prepared.Lines[2]);
        Assert.Equal(["same", "changed", "last"], prepared.Lines);
        Assert.True(prepared.EndsWithNewLine);
    }

    [Fact]
    public void Prepare_WithBaseline_PreservesEmptyAndShiftedLineSemantics()
    {
        var baseline = LineDiffEngine.Prepare("alpha\n\nbeta\ngamma");

        var prepared = LineDiffEngine.Prepare("inserted\nalpha\n\nbeta\ngamma", baseline);

        Assert.Equal(["inserted", "alpha", "", "beta", "gamma"], prepared.Lines);
        Assert.False(prepared.EndsWithNewLine);
    }

    [Fact]
    public void Prepare_WithBaseline_AvoidsDuplicateStringsForLocalizedConflictEdits()
    {
        var baseText = CreateText();
        var currentText = baseText.Replace("line 10000 stable", "line 10000 current", StringComparison.Ordinal);
        var incomingText = baseText.Replace("line 10000 stable", "line 10000 incoming", StringComparison.Ordinal);
        var baseline = LineDiffEngine.Prepare(baseText);
        _ = LineDiffEngine.Prepare(currentText, baseline);
        _ = LineDiffEngine.Prepare(incomingText, baseline);

        var separate = Measure(() =>
        {
            GC.KeepAlive(LineDiffEngine.Prepare(currentText));
            GC.KeepAlive(LineDiffEngine.Prepare(incomingText));
        });
        var reused = Measure(() =>
        {
            GC.KeepAlive(LineDiffEngine.Prepare(currentText, baseline));
            GC.KeepAlive(LineDiffEngine.Prepare(incomingText, baseline));
        });

        output.WriteLine($"Separate: {separate.Elapsed.TotalMilliseconds:N1} ms, {separate.Allocated:N0} bytes");
        output.WriteLine($"Reused: {reused.Elapsed.TotalMilliseconds:N1} ms, {reused.Allocated:N0} bytes");
        Assert.True(reused.Allocated + 1_000_000 < separate.Allocated);
    }

    private static string CreateText() => string.Join(
        '\n',
        Enumerable.Range(0, 20_000).Select(index => $"line {index} stable"));

    private static Measurement Measure(Action action)
    {
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        action();
        return new(Stopwatch.GetElapsedTime(started), GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private readonly record struct Measurement(TimeSpan Elapsed, long Allocated);
}
