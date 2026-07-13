using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Diffing;
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
            shared.Allocated < duplicated.Allocated * 0.80,
            $"Prepared path allocated {shared.Allocated:N0} vs {duplicated.Allocated:N0} bytes.");
        Assert.True(
            shared.Allocated < 1_050_000,
            $"Prepared conflict rendering allocated {shared.Allocated:N0} bytes.");
    }

    [Fact]
    public void WhitespaceVariant_ReusingExactHunksAvoidsDuplicateExactDiffWork()
    {
        var fixture = CreateFixture(5_000);
        var exact = PrepareShared(fixture);
        _ = PrepareWhitespaceFromScratch(fixture);
        _ = PrepareWhitespaceWithHunks(fixture, exact.Hunks);

        var fromScratch = Measure(() => PrepareWhitespaceFromScratch(fixture));
        var reused = Measure(() => PrepareWhitespaceWithHunks(fixture, exact.Hunks));

        output.WriteLine($"Whitespace from scratch: {fromScratch.Elapsed.TotalMilliseconds:N1} ms, {fromScratch.Allocated:N0} bytes");
        output.WriteLine($"Whitespace with exact hunks: {reused.Elapsed.TotalMilliseconds:N1} ms, {reused.Allocated:N0} bytes");
        Assert.True(
            reused.Allocated < fromScratch.Allocated * 0.75,
            $"Reused path allocated {reused.Allocated:N0} vs {fromScratch.Allocated:N0} bytes.");
    }

    [Fact]
    public void LineDiff_KeepsLargeSparseComparisonWithinAllocationBudget()
    {
        var fixture = CreateFixture(5_000);
        _ = ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Current);
        var lineDiff = Measure(() => ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Current));
        output.WriteLine($"Line diff: {lineDiff.Elapsed.TotalMilliseconds:N1} ms, {lineDiff.Allocated:N0} bytes");
        Assert.True(lineDiff.Allocated < 1_000_000, $"Line diff allocated {lineDiff.Allocated:N0} bytes.");
    }

    [Fact]
    public void SharedPreparedBase_ReducesTwoSidedComparisonAllocations()
    {
        var fixture = CreateFixture(5_000);
        _ = BuildTwoDiffsDuplicated(fixture);
        _ = BuildTwoDiffsPrepared(fixture);

        var duplicated = Measure(() => BuildTwoDiffsDuplicated(fixture));
        var prepared = Measure(() => BuildTwoDiffsPrepared(fixture));

        output.WriteLine($"Duplicated lines: {duplicated.Allocated:N0} bytes");
        output.WriteLine($"Prepared lines: {prepared.Allocated:N0} bytes");
        Assert.True(
            prepared.Allocated < duplicated.Allocated * 0.90,
            $"Prepared path allocated {prepared.Allocated:N0} vs {duplicated.Allocated:N0} bytes.");
    }

    [Fact]
    public void ExactSizedLineSplit_AvoidsNormalizationAndTrailingArrayCopy()
    {
        var text = string.Join("\r\n", Enumerable.Range(0, 5_000).Select(index => $"line {index}")) + "\r\n";
        _ = LegacySplitLines(text);
        _ = LineDiffEngine.SplitLines(text);

        var legacy = Measure(() => LegacySplitLines(text));
        var exact = Measure(() => LineDiffEngine.SplitLines(text));

        output.WriteLine($"Legacy split: {legacy.Allocated:N0} bytes");
        output.WriteLine($"Exact split: {exact.Allocated:N0} bytes");
        Assert.True(
            exact.Allocated < legacy.Allocated * 0.75,
            $"Exact split allocated {exact.Allocated:N0} vs {legacy.Allocated:N0} bytes.");
    }

    private static string[] LegacySplitLines(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n');
        return normalized.EndsWith('\n') ? lines[..^1] : lines;
    }

    private static object BuildTwoDiffsDuplicated(Fixture fixture) => new[]
    {
        ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Current, ignoreWhitespace: true),
        ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Incoming, ignoreWhitespace: true),
    };

    private static object BuildTwoDiffsPrepared(Fixture fixture)
    {
        var baseLines = ConflictHunkBuilder.PrepareLineText(fixture.Base);
        var currentLines = ConflictHunkBuilder.PrepareLineText(fixture.Current);
        var incomingLines = ConflictHunkBuilder.PrepareLineText(fixture.Incoming);
        return new[]
        {
            ConflictHunkBuilder.BuildLineModel(baseLines, currentLines, ignoreWhitespace: true),
            ConflictHunkBuilder.BuildLineModel(baseLines, incomingLines, ignoreWhitespace: true),
        };
    }

    private static PreparedConflict PrepareShared(Fixture fixture)
    {
        var currentModel = ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Current);
        var incomingModel = ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Incoming);
        return new PreparedConflict(
            ConflictHunkBuilder.Build(
                fixture.Result,
                currentModel,
                incomingModel),
            BuildPrepared(fixture.Base, fixture.Current, currentModel),
            BuildPrepared(fixture.Base, fixture.Incoming, incomingModel));
    }

    private static PreparedConflict PrepareWhitespaceFromScratch(Fixture fixture)
    {
        var exactCurrent = ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Current);
        var exactIncoming = ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Incoming);
        var hunks = ConflictHunkBuilder.Build(
            fixture.Result,
            exactCurrent,
            exactIncoming);
        return PrepareWhitespaceWithHunks(fixture, hunks);
    }

    private static PreparedConflict PrepareWhitespaceWithHunks(
        Fixture fixture,
        List<ExpressThat.LovelyGit.Services.Git.WorkingTree.Models.ConflictHunk> hunks)
    {
        var current = ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Current, ignoreWhitespace: true);
        var incoming = ConflictHunkBuilder.BuildLineModel(fixture.Base, fixture.Incoming, ignoreWhitespace: true);
        return new PreparedConflict(
            hunks,
            BuildPrepared(fixture.Base, fixture.Current, current),
            BuildPrepared(fixture.Base, fixture.Incoming, incoming));
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
        LineDiffModel model) =>
        WorkingTreeChangeService.BuildPreparedLineDiffResponse(
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
    private sealed record PreparedConflict(
        List<ExpressThat.LovelyGit.Services.Git.WorkingTree.Models.ConflictHunk> Hunks,
        CommitFileDiffResponse Current,
        CommitFileDiffResponse Incoming);
    private readonly record struct Measurement(TimeSpan Elapsed, long Allocated);
}
