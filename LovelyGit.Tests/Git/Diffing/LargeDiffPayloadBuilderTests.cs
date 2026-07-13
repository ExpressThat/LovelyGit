using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

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
        Assert.True(response.Lines.Count > DiffInputGuard.FastDiffInputLines);
        Assert.Contains(response.Lines, line => line.ChangeType == "Deleted" && line.Text == "line 10");
        Assert.Contains(response.Lines, line => line.ChangeType == "Inserted" && line.Text == "changed 10");
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

        var inserted = Assert.Single(response.Lines, line => line.ChangeType == "Inserted");
        Assert.Equal(3, inserted.NewLineNumber);
        Assert.Equal("three", inserted.NewText);
        Assert.Null(inserted.Text);
        Assert.Null(inserted.SyntaxSpans);
        Assert.Null(inserted.ChangeSpans);
    }

    [Fact]
    public void Build_ReturnsVirtualTextForLargeAddedFile()
    {
        var newText = string.Join('\n', Enumerable.Range(1, DiffInputGuard.FastDiffInputLines + 1).Select(index => $"line {index}"));

        var response = LargeDiffPayloadBuilder.Build("hash", "large.txt", "Added", CommitDiffViewMode.SideBySide, false, string.Empty, newText);

        Assert.Empty(response.Lines);
        Assert.Equal("Inserted", response.VirtualChangeType);
        Assert.Equal(newText, response.VirtualText);
        Assert.Equal(DiffInputGuard.FastDiffInputLines + 1, response.VirtualLineCount);
    }
}
