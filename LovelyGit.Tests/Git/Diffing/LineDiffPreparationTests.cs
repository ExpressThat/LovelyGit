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
        _ = LegacyPrepare(currentText);
        _ = LegacyPrepare(incomingText);

        var scalar = Measure(() =>
        {
            GC.KeepAlive(LegacyPrepare(currentText));
            GC.KeepAlive(LegacyPrepare(incomingText));
        });
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

        output.WriteLine($"Scalar: {scalar.Elapsed.TotalMilliseconds:N1} ms, {scalar.Allocated:N0} bytes");
        output.WriteLine($"Separate: {separate.Elapsed.TotalMilliseconds:N1} ms, {separate.Allocated:N0} bytes");
        output.WriteLine($"Reused: {reused.Elapsed.TotalMilliseconds:N1} ms, {reused.Allocated:N0} bytes");
        Assert.True(
            separate.Elapsed < scalar.Elapsed * 0.75,
            $"Vectorized preparation took {separate.Elapsed.TotalMilliseconds:N1} vs " +
            $"{scalar.Elapsed.TotalMilliseconds:N1} ms scalar.");
        Assert.True(
            reused.Allocated + 1_000_000 < separate.Allocated,
            $"Reused preparation allocated {reused.Allocated:N0} vs {separate.Allocated:N0} bytes.");
    }

    private static string CreateText() => string.Join(
        '\n',
        Enumerable.Range(0, 20_000).Select(index => $"line {index} stable"));

    private static PreparedLineText LegacyPrepare(string text)
    {
        var separatorCount = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] is not ('\r' or '\n')) continue;
            separatorCount++;
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
        }
        var endsWithNewLine = text.EndsWith('\n') || text.EndsWith('\r');
        var lines = new string[separatorCount + (endsWithNewLine ? 0 : 1)];
        var lineStart = 0;
        var lineIndex = 0;
        for (var index = 0; index < text.Length && lineIndex < lines.Length; index++)
        {
            if (text[index] is not ('\r' or '\n')) continue;
            lines[lineIndex++] = text.Substring(lineStart, index - lineStart);
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
            lineStart = index + 1;
        }
        if (!endsWithNewLine) lines[^1] = text[lineStart..];
        return new(lines, endsWithNewLine);
    }

    private static Measurement Measure(Action action)
    {
        const int repetitions = 5;
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < repetitions; index++) action();
        return new(Stopwatch.GetElapsedTime(started), GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private readonly record struct Measurement(TimeSpan Elapsed, long Allocated);
}
