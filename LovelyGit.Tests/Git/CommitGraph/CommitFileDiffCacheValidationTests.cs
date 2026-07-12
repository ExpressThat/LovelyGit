using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitFileDiffCacheValidationTests
{
    [Fact]
    public void ChangedTextWithoutAnyPayload_IsInvalid()
    {
        var response = new CommitFileDiffResponse
        {
            Status = "Added",
            HasDifferences = true,
        };

        Assert.False(CommitFileDiffService.IsValidCachedDiff(response));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void NonTextOrUnchangedResponses_DoNotRequireRows(
        bool hasDifferences,
        bool isBinary)
    {
        var response = new CommitFileDiffResponse
        {
            HasDifferences = hasDifferences,
            IsBinary = isBinary,
        };

        Assert.True(CommitFileDiffService.IsValidCachedDiff(response));
    }

    [Fact]
    public void CompactPayload_IsValidWithoutExpandedRows()
    {
        var response = new CommitFileDiffResponse
        {
            HasDifferences = true,
            CompactLineCount = 2_446,
            CompactLinesGzipBase64 = "payload",
        };

        Assert.True(CommitFileDiffService.IsValidCachedDiff(response));
    }
}
