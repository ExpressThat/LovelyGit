using System.Text;
using System.Text.Json;
using System.IO.Compression;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class PreparedConflictComparisonTests
{
    [Fact]
    public void PreparedResponse_MatchesRegularSideBySideResponse()
    {
        const string oldText = "before\nold value\nafter\n";
        const string newText = "before\nnew value\nafter\n";
        var expected = WorkingTreeChangeService.BuildDiffResponse(
            "CONFLICT",
            "sample.cs",
            "Unmerged",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false,
            Encoding.UTF8.GetBytes(oldText),
            Encoding.UTF8.GetBytes(newText),
            compact: false);

        var actual = WorkingTreeChangeService.BuildPreparedLineDiffResponse(
            "CONFLICT",
            "sample.cs",
            "Unmerged",
            oldText,
            newText,
            ConflictHunkBuilder.BuildLineModel(oldText, newText));

        Assert.Equal(expected.HasDifferences, actual.HasDifferences);
        Assert.Equal(JsonSerializer.Serialize(expected.Lines), JsonSerializer.Serialize(actual.Lines));
    }

    [Fact]
    public void PreparedWhitespaceResponse_PreservesIgnoredWhitespaceSemantics()
    {
        const string oldText = "value = 1;\n";
        const string newText = "value  =  1;\n";
        var model = ConflictHunkBuilder.BuildLineModel(oldText, newText, ignoreWhitespace: true);

        var actual = WorkingTreeChangeService.BuildPreparedLineDiffResponse(
            "CONFLICT",
            "sample.cs",
            "Unmerged",
            oldText,
            newText,
            model);

        Assert.False(actual.HasDifferences);
        Assert.All(actual.Lines, line => Assert.Equal("Unchanged", line.ChangeType));
    }

    [Fact]
    public void PreparedLargeResponse_StreamsTheSameCompactRowsAsExpandedRendering()
    {
        var oldText = string.Join('\n', Enumerable.Range(1, 800).Select(index => $"line {index}"));
        var newText = oldText.Replace("line 400", "changed 400", StringComparison.Ordinal);
        var expected = WorkingTreeChangeService.BuildDiffResponse(
            "CONFLICT",
            "sample.txt",
            "Unmerged",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false,
            Encoding.UTF8.GetBytes(oldText),
            Encoding.UTF8.GetBytes(newText),
            compact: false);
        ConflictComparisonPayloadBuilder.Compact(expected);

        var actual = WorkingTreeChangeService.BuildPreparedLineDiffResponse(
            "CONFLICT",
            "sample.txt",
            "Unmerged",
            oldText,
            newText,
            ConflictHunkBuilder.BuildLineModel(oldText, newText));

        Assert.Empty(actual.Lines);
        Assert.Equal(expected.CompactLineCount, actual.CompactLineCount);
        Assert.Equal(Decode(expected.CompactLinesGzipBase64), Decode(actual.CompactLinesGzipBase64));
    }

    private static string Decode(string payload)
    {
        var bytes = Convert.FromBase64String(payload);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
