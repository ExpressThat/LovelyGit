using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using System.IO.Compression;
using System.Text.Json;

namespace LovelyGit.Tests.Git.Diffing;

public sealed class LargeDiffPayloadBuilderTests
{
    [Fact]
    public void Build_ReturnsFullCombinedDiffWithoutTruncatingLargeInput()
    {
        var oldText = string.Join('\n', Enumerable.Range(0, DiffInputGuard.FastDiffInputLines + 1).Select(index => $"line {index}"));
        var newText = oldText.Replace("line 10", "changed 10", StringComparison.Ordinal);

        var response = LargeDiffPayloadBuilder.Build("hash", "large.txt", "Modified", CommitDiffViewMode.Combined, false, oldText, newText);

        Assert.False(response.IsTruncated);
        Assert.Empty(response.Lines);
        Assert.Equal(ReferencedDiffPayloadBuilder.LineSchema, response.CompactLineSchema);
        Assert.True(response.CompactLineCount > DiffInputGuard.FastDiffInputLines);
        Assert.NotEmpty(response.CompactLinesGzipBase64);
        using var compactLines = ExpandLines(response.CompactLinesGzipBase64);
        Assert.Contains(compactLines.RootElement.EnumerateArray(), row => row[2].GetInt32() == 2);
        Assert.Contains(compactLines.RootElement.EnumerateArray(), row => row[2].GetInt32() == 3);
        var sources = ConflictTextBundleCodec.Expand(response.CompactSourceBundleGzipBase64);
        Assert.Equal(oldText, sources.Base);
        Assert.Equal(newText, sources.Ours);
    }

    [Fact]
    public void Build_ReturnsSideBySideInsertionsAndDeletes()
    {
        var response = LargeDiffPayloadBuilder.Build(
            "hash",
            "file.txt",
            "Modified",
            CommitDiffViewMode.SideBySide,
            false,
            "one\ntwo\nfour",
            "one\ntwo\nthree\nfour");

        Assert.Empty(response.Lines);
        Assert.Equal(4, response.CompactLineCount);
        Assert.Equal(ReferencedDiffPayloadBuilder.LineSchema, response.CompactLineSchema);
        var sources = ConflictTextBundleCodec.Expand(response.CompactSourceBundleGzipBase64);
        Assert.Equal("one\ntwo\nfour", sources.Base);
        Assert.Equal("one\ntwo\nthree\nfour", sources.Ours);
    }

    [Fact]
    public void Build_KeepsLargeMostlyUnchangedPayloadBelowWebViewMessageBudget()
    {
        var oldText = string.Join('\n', Enumerable.Range(1, 80_000).Select(index => $"line {index:D5} repeated content"));
        var newText = oldText.Replace("line 40000", "changed 40000", StringComparison.Ordinal);

        var response = LargeDiffPayloadBuilder.Build(
            "hash", "large.txt", "Modified", CommitDiffViewMode.SideBySide, false, oldText, newText);

        var encodedCharacters = response.CompactLinesGzipBase64.Length
            + response.CompactSourceBundleGzipBase64.Length;
        Assert.True(encodedCharacters < 1_000_000, $"Compact payload was {encodedCharacters:N0} characters.");
    }

    [Fact]
    public void Build_ReturnsVirtualTextForLargeAddedFile()
    {
        var newText = string.Join(
            '\n',
            Enumerable.Range(1, DiffInputGuard.VirtualTextInputLines).Select(index => $"line {index}"));

        var response = LargeDiffPayloadBuilder.Build("hash", "large.txt", "Added", CommitDiffViewMode.SideBySide, false, string.Empty, newText);

        Assert.Empty(response.Lines);
        Assert.Equal("Inserted", response.VirtualChangeType);
        Assert.Equal(newText, response.VirtualText);
        Assert.Equal(DiffInputGuard.VirtualTextInputLines, response.VirtualLineCount);
    }

    [Fact]
    public void Build_CompressesLargeAddedVirtualText()
    {
        var newText = new string('x', DiffInputGuard.VirtualTextInputCharacters);

        var response = LargeDiffPayloadBuilder.Build(
            "hash", "large.txt", "Added", CommitDiffViewMode.SideBySide, false, string.Empty, newText);

        Assert.Null(response.VirtualText);
        Assert.Equal("gzip-base64:utf-8", response.VirtualTextEncoding);
        Assert.NotEmpty(response.VirtualTextGzipBase64);
    }

    private static JsonDocument ExpandLines(string payload)
    {
        using var input = new MemoryStream(Convert.FromBase64String(payload));
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        return JsonDocument.Parse(gzip);
    }
}
