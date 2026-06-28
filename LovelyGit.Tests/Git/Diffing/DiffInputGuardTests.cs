using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace LovelyGit.Tests.Git.Diffing;

public sealed class DiffInputGuardTests
{
    [Fact]
    public void ShouldTruncate_ReturnsFalseForSmallInputs()
    {
        var shouldTruncate = DiffInputGuard.ShouldTruncate("old\n", "new\n");

        Assert.False(shouldTruncate);
    }

    [Fact]
    public void ShouldTruncate_ReturnsTrueWhenCharacterLimitIsExceeded()
    {
        var oversizedText = new string('a', DiffInputGuard.MaxDiffInputCharacters + 1);

        var shouldTruncate = DiffInputGuard.ShouldTruncate(oversizedText, string.Empty);

        Assert.True(shouldTruncate);
    }

    [Fact]
    public void ShouldTruncate_ReturnsTrueWhenLineLimitIsExceeded()
    {
        var oversizedText = string.Join('\n', Enumerable.Repeat("line", DiffInputGuard.MaxDiffInputLines + 1));

        var shouldTruncate = DiffInputGuard.ShouldTruncate(oversizedText, string.Empty);

        Assert.True(shouldTruncate);
    }

    [Fact]
    public void BuildTruncatedResponse_ReturnsLightweightDiffResponse()
    {
        var response = DiffInputGuard.BuildTruncatedResponse(
            "abc123",
            "large.txt",
            "Modified",
            CommitDiffViewMode.Combined,
            "old",
            "new");

        Assert.Equal("abc123", response.CommitHash);
        Assert.Equal("large.txt", response.Path);
        Assert.Equal("Modified", response.Status);
        Assert.Equal(CommitDiffViewMode.Combined, response.ViewMode);
        Assert.True(response.HasDifferences);
        Assert.True(response.IsTruncated);
        Assert.Empty(response.Lines);
        Assert.Contains("too large", response.TruncationMessage);
    }
}
