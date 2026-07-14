using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace LovelyGit.Tests.Git.Diffing;

public sealed class LineDiffRenderingTests
{
    private static readonly LineDiffRow ChangedRow = new(0, 0, isChanged: true);

    [Theory]
    [InlineData("prefix old suffix", "prefix new suffix", 7, 3, 7, 3)]
    [InlineData("prefix suffix", "prefix new suffix", -1, 0, 7, 4)]
    [InlineData("prefix old suffix", "prefix suffix", 7, 4, -1, 0)]
    [InlineData("aaaaXaaaa", "aaaaYaaaa", 4, 1, 4, 1)]
    public void ChangeSpans_KeepAbsoluteOffsetsAfterTrimmingUnchangedEdges(
        string oldText,
        string newText,
        int oldStart,
        int oldLength,
        int newStart,
        int newLength)
    {
        var spans = LineDiffRendering.ChangeSpans(oldText, newText, ChangedRow);

        AssertSpan(spans.Old, oldStart, oldLength, "Deleted");
        AssertSpan(spans.New, newStart, newLength, "Inserted");
    }

    [Fact]
    public void ChangeSpans_ReturnsNoChangesForAnUnchangedRow()
    {
        var spans = LineDiffRendering.ChangeSpans("same", "same", new(0, 0, isChanged: false));

        Assert.Empty(spans.Old);
        Assert.Empty(spans.New);
    }

    [Fact]
    public void ChangeSpans_StillHighlightsTheWholeOneSidedLine()
    {
        var inserted = LineDiffRendering.ChangeSpans(string.Empty, "new line", new(null, 0, true));
        var deleted = LineDiffRendering.ChangeSpans("old line", string.Empty, new(0, null, true));

        AssertSpan(inserted.New, 0, 8, "Inserted");
        AssertSpan(deleted.Old, 0, 8, "Deleted");
    }

    [Fact]
    public void ChangeSpans_PreservesOffsetsAcrossMultipleMiddleEdits()
    {
        var spans = LineDiffRendering.ChangeSpans(
            "aaa one bbb two ccc",
            "aaa ONE bbb TWO ccc",
            ChangedRow);

        Assert.Collection(
            spans.Old,
            span => Assert.Equal((4, 3), (span.Start, span.Length)),
            span => Assert.Equal((12, 3), (span.Start, span.Length)));
        Assert.Collection(
            spans.New,
            span => Assert.Equal((4, 3), (span.Start, span.Length)),
            span => Assert.Equal((12, 3), (span.Start, span.Length)));
    }

    private static void AssertSpan(
        IReadOnlyList<CommitFileDiffChangeSpan> spans,
        int start,
        int length,
        string changeType)
    {
        if (start < 0)
        {
            Assert.Empty(spans);
            return;
        }
        var span = Assert.Single(spans);
        Assert.Equal((start, length, changeType), (span.Start, span.Length, span.ChangeType));
    }
}
