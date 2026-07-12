using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictPreparationPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void PreparedModels_AvoidDuplicateDiffWorkAndAllocations()
    {
        var fixture = CreateFixture(2_000);
        _ = PrepareShared(fixture);
        _ = PrepareDuplicated(fixture);

        var shared = Measure(() => PrepareShared(fixture));
        var duplicated = Measure(() => PrepareDuplicated(fixture));

        output.WriteLine($"Shared: {shared.Elapsed.TotalMilliseconds:N1} ms, {shared.Allocated:N0} bytes");
        output.WriteLine($"Duplicated: {duplicated.Elapsed.TotalMilliseconds:N1} ms, {duplicated.Allocated:N0} bytes");
        Assert.True(
            shared.Allocated < duplicated.Allocated * 0.75,
            $"Prepared path allocated {shared.Allocated:N0} vs {duplicated.Allocated:N0} bytes.");
    }

    private static object PrepareShared(Fixture fixture)
    {
        var currentModel = ConflictHunkBuilder.BuildModel(fixture.Base, fixture.Current);
        var incomingModel = ConflictHunkBuilder.BuildModel(fixture.Base, fixture.Incoming);
        return new
        {
            Hunks = ConflictHunkBuilder.Build(
                fixture.Current,
                fixture.Incoming,
                fixture.Result,
                currentModel,
                incomingModel),
            Current = BuildPrepared(fixture.Base, fixture.Current, currentModel),
            Incoming = BuildPrepared(fixture.Base, fixture.Incoming, incomingModel),
        };
    }

    private static object PrepareDuplicated(Fixture fixture)
    {
        return new
        {
            Hunks = ConflictHunkBuilder.Build(
                fixture.Base,
                fixture.Current,
                fixture.Incoming,
                fixture.Result),
            Current = BuildRegular(fixture.Base, fixture.Current),
            Incoming = BuildRegular(fixture.Base, fixture.Incoming),
        };
    }

    private static CommitFileDiffResponse BuildPrepared(
        string oldText,
        string newText,
        DiffPlex.DiffBuilder.Model.SideBySideDiffModel model) =>
        WorkingTreeChangeService.BuildPreparedSideBySideResponse(
            "CONFLICT", "fixture.txt", "Unmerged", oldText, newText, model);

    private static CommitFileDiffResponse BuildRegular(string oldText, string newText) =>
        WorkingTreeChangeService.BuildDiffResponse(
            "CONFLICT",
            "fixture.txt",
            "Unmerged",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false,
            Encoding.UTF8.GetBytes(oldText),
            Encoding.UTF8.GetBytes(newText),
            compact: false);

    private static Measurement Measure(Func<object> action)
    {
        GC.Collect();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var startedAt = Stopwatch.GetTimestamp();
        GC.KeepAlive(action());
        return new Measurement(
            Stopwatch.GetElapsedTime(startedAt),
            GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
    }

    private static Fixture CreateFixture(int lineCount)
    {
        var common = string.Join('\n', Enumerable.Range(0, lineCount).Select(index => $"line {index}"));
        var baseText = common.Replace("line 1000", "base", StringComparison.Ordinal) + '\n';
        var current = common.Replace("line 1000", "current", StringComparison.Ordinal) + '\n';
        var incoming = common.Replace("line 1000", "incoming", StringComparison.Ordinal) + '\n';
        var result = common.Replace(
            "line 1000",
            "<<<<<<< HEAD\ncurrent\n=======\nincoming\n>>>>>>> feature",
            StringComparison.Ordinal) + '\n';
        return new Fixture(baseText, current, incoming, result);
    }

    private sealed record Fixture(string Base, string Current, string Incoming, string Result);
    private readonly record struct Measurement(TimeSpan Elapsed, long Allocated);
}
