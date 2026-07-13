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
}
