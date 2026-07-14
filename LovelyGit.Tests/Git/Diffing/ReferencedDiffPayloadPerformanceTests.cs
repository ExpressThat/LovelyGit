using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Diffing;

[Collection(PerformanceTestCollection.Name)]
public sealed class ReferencedDiffPayloadPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void LongLocalizedChange_KeepRowPayloadAllocationBounded()
    {
        var oldText = new string('a', 100_000) + "old" + new string('z', 100_000);
        var newText = new string('a', 100_000) + "new" + new string('z', 100_000);
        var model = LineDiffEngine.BuildUnaligned(oldText, newText);
        _ = Build(model);

        var measured = BestOfThree(() => Build(model));

        output.WriteLine(
            $"Referenced rows: {measured.Elapsed.TotalMilliseconds:N1} ms, " +
            $"{measured.Allocated:N0} bytes, {measured.PayloadCharacters:N0} characters");
        Assert.True(
            measured.Allocated < 1_000_000,
            $"Referenced rows allocated {measured.Allocated:N0} bytes.");
        Assert.True(
            measured.Elapsed < TimeSpan.FromMilliseconds(10),
            $"Referenced rows took {measured.Elapsed.TotalMilliseconds:N1} ms.");
    }

    private static string Build(LineDiffModel model) =>
        ReferencedDiffPayloadBuilder.Build(
            "hash", "large.txt", "Modified", CommitDiffViewMode.SideBySide, model)
        .CompactLinesGzipBase64;

    private static Measurement BestOfThree(Func<string> action)
    {
        var best = Measure(action);
        for (var index = 1; index < 3; index++)
        {
            var candidate = Measure(action);
            if (candidate.Elapsed < best.Elapsed) best = candidate;
        }
        return best;
    }

    private static Measurement Measure(Func<string> action)
    {
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        var payload = action();
        return new(
            Stopwatch.GetElapsedTime(started),
            GC.GetAllocatedBytesForCurrentThread() - before,
            payload.Length);
    }

    private readonly record struct Measurement(
        TimeSpan Elapsed,
        long Allocated,
        int PayloadCharacters);
}
