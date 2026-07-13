using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class BlobLineAnalyzerTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("one", 1)]
    [InlineData("one\n", 1)]
    [InlineData("one\ntwo", 2)]
    [InlineData("one\r\ntwo\r\n", 2)]
    public void Summarize_CountsTextLinesWithoutFingerprints(string text, int expected)
    {
        var summary = BlobLineAnalyzer.Summarize(Encoding.UTF8.GetBytes(text));

        Assert.False(summary.IsBinary);
        Assert.Equal(expected, summary.LineCount);
    }

    [Fact]
    public void Summarize_RecognizesBinaryContentWithoutCountingLines()
    {
        var summary = BlobLineAnalyzer.Summarize([1, 2, 0, 10, 3]);

        Assert.True(summary.IsBinary);
        Assert.Equal(0, summary.LineCount);
    }

    [Fact]
    public void CalculateLineStats_PreservesDuplicateLineMultiplicityOnSmallFiles()
    {
        var first = new LineFingerprint(1, 4);
        var second = new LineFingerprint(2, 5);
        var oldBlob = new BlobAnalysis(false, [first, first, second]);
        var newBlob = new BlobAnalysis(false, [first, second, second]);

        var stats = BlobLineAnalyzer.CalculateLineStats(oldBlob, newBlob);

        Assert.Equal((1u, 1u), stats);
    }

    [Fact]
    public void CalculateLineStats_PreservesDuplicateLineMultiplicityOnLargeFiles()
    {
        var shared = new LineFingerprint(1, 4);
        var removed = new LineFingerprint(2, 5);
        var added = new LineFingerprint(3, 5);
        var oldBlob = new BlobAnalysis(false, [.. Enumerable.Repeat(shared, 16), removed]);
        var newBlob = new BlobAnalysis(false, [.. Enumerable.Repeat(shared, 16), added]);

        var stats = BlobLineAnalyzer.CalculateLineStats(oldBlob, newBlob);

        Assert.Equal((1u, 1u), stats);
    }
}
