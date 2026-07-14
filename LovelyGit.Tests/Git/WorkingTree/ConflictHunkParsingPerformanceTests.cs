using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(PerformanceTestCollection.Name)]
public sealed class ConflictHunkParsingPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void SparseLargeConflict_DoesNotSplitUnchangedResultLines()
    {
        var common = string.Join('\n', Enumerable.Range(0, 20_000).Select(index => $"line {index:D5}"));
        var baseText = common.Replace("line 10000", "base", StringComparison.Ordinal) + '\n';
        var current = common.Replace("line 10000", "current", StringComparison.Ordinal) + '\n';
        var incoming = common.Replace("line 10000", "incoming", StringComparison.Ordinal) + '\n';
        var result = common.Replace(
            "line 10000",
            "<<<<<<< HEAD\ncurrent\n=======\nincoming\n>>>>>>> feature",
            StringComparison.Ordinal) + '\n';
        var currentModel = ConflictHunkBuilder.BuildLineModel(baseText, current);
        var incomingModel = ConflictHunkBuilder.BuildLineModel(baseText, incoming);
        _ = ConflictHunkBuilder.Build(result, currentModel, incomingModel);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        var hunks = ConflictHunkBuilder.Build(result, currentModel, incomingModel);
        var elapsed = Stopwatch.GetElapsedTime(started);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        output.WriteLine($"Range parser: {elapsed.TotalMilliseconds:N1} ms, {allocated:N0} bytes");
        Assert.Single(hunks);
        Assert.True(allocated < 100_000, $"Hunk parsing allocated {allocated:N0} bytes.");
    }
}
