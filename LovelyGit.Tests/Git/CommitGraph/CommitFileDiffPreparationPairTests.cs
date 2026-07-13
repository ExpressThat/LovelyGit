using System.Text.Json;
using ColorCode;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitFileDiffPreparationPairTests(ITestOutputHelper output)
{
    [Fact]
    public void Pair_matches_independently_rendered_views()
    {
        var source = Source(
            "alpha\nbefore value\nremoved\ntail\n",
            "alpha\nafter value\ninserted\ntail\nextra\n") with
        {
            Language = Languages.CSharp,
        };

        var expectedSide = Build(source, CommitDiffViewMode.SideBySide);
        var expectedCombined = Build(source, CommitDiffViewMode.Combined);
        var pair = CommitFileDiffService.BuildResponsePairFromSource("abc123", "sample.txt", source);

        Assert.Equal(JsonSerializer.Serialize(expectedSide), JsonSerializer.Serialize(pair.SideBySide));
        Assert.Equal(JsonSerializer.Serialize(expectedCombined), JsonSerializer.Serialize(pair.Combined));
    }

    [Fact]
    public void Pair_reuses_virtual_payload_for_large_added_file()
    {
        var source = Source(string.Empty, new string('x', 80_000));

        var pair = CommitFileDiffService.BuildResponsePairFromSource("abc123", "large.txt", source);

        Assert.Equal(CommitDiffViewMode.SideBySide, pair.SideBySide.ViewMode);
        Assert.Equal(CommitDiffViewMode.Combined, pair.Combined.ViewMode);
        Assert.Same(
            pair.SideBySide.VirtualTextGzipBase64,
            pair.Combined.VirtualTextGzipBase64);
    }

    [Fact]
    public void Pair_allocates_less_than_two_independent_renders()
    {
        var source = Source(BuildText("before"), BuildText("after"));
        _ = CommitFileDiffService.BuildResponsePairFromSource("abc123", "sample.txt", source);
        _ = Build(source, CommitDiffViewMode.SideBySide);
        _ = Build(source, CommitDiffViewMode.Combined);

        var independentBytes = MeasureIndependent(source);
        var pairBytes = MeasurePair(source);
        output.WriteLine(
            "Paired preparation: {0:N0} bytes; independent preparation: {1:N0} bytes.",
            pairBytes,
            independentBytes);

        Assert.True(
            pairBytes < independentBytes,
            $"Expected paired preparation to allocate less; pair={pairBytes:N0}, independent={independentBytes:N0}.");
    }

    private static CommitFileDiffService.CommitFileDiffSource Source(
        string oldText,
        string newText) => new()
        {
            Status = "Modified",
            OldText = oldText,
            NewText = newText,
        };

    private static CommitFileDiffResponse Build(
        CommitFileDiffService.CommitFileDiffSource source,
        CommitDiffViewMode mode) => CommitFileDiffService.BuildResponseFromSource(
            "abc123",
            "sample.txt",
            mode,
            ignoreWhitespace: false,
            source);

    private static string BuildText(string changed)
    {
        var lines = Enumerable.Range(0, 500)
            .Select(index => index % 20 == 0 ? $"{changed} {index}" : $"stable line {index}");
        return string.Join('\n', lines);
    }

    private static long MeasureIndependent(CommitFileDiffService.CommitFileDiffSource source)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var side = Build(source, CommitDiffViewMode.SideBySide);
        var combined = Build(source, CommitDiffViewMode.Combined);
        GC.KeepAlive(side);
        GC.KeepAlive(combined);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static long MeasurePair(CommitFileDiffService.CommitFileDiffSource source)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var pair = CommitFileDiffService.BuildResponsePairFromSource("abc123", "sample.txt", source);
        GC.KeepAlive(pair.SideBySide);
        GC.KeepAlive(pair.Combined);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
