using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace LovelyGit.Tests.Data;

public sealed class CommitFileDiffCacheRepositoryTests
{
    [Fact]
    public void ToResponse_PreservesCompactColoredPayload()
    {
        var cached = CommitFileDiffCacheRepository.CreateCacheEntry(
            "id",
            Guid.NewGuid(),
            "commit",
            "large.cc",
            new CommitFileDiffResponse
            {
                Status = "Modified",
                ViewMode = CommitDiffViewMode.SideBySide,
                HasDifferences = true,
                CompactLineSchema = "tuple-v2:gzip-base64:utf-8",
                CompactLinesGzipBase64 = "colored-payload",
                CompactLineCount = 1_248,
            },
            ignoreWhitespace: false);
        var response = CommitFileDiffCacheRepository.ToResponse(
            cached,
            []);

        Assert.Equal("tuple-v2:gzip-base64:utf-8", response.CompactLineSchema);
        Assert.Equal("colored-payload", response.CompactLinesGzipBase64);
        Assert.Equal(1_248, response.CompactLineCount);
        Assert.Empty(response.Lines);
    }

    [Fact]
    public void ToResponse_PreservesVirtualAndTruncationMetadata()
    {
        var response = CommitFileDiffCacheRepository.ToResponse(
            new CommitFileDiffCacheEntry
            {
                IsTruncated = true,
                TruncationMessage = "too large",
                VirtualTextGzipBase64 = "virtual-payload",
                VirtualTextEncoding = "gzip-base64:utf-8",
                VirtualChangeType = "Added",
                VirtualLineCount = 5_000,
            },
            []);

        Assert.True(response.IsTruncated);
        Assert.Equal("too large", response.TruncationMessage);
        Assert.Equal("virtual-payload", response.VirtualTextGzipBase64);
        Assert.Equal("gzip-base64:utf-8", response.VirtualTextEncoding);
        Assert.Equal("Added", response.VirtualChangeType);
        Assert.Equal(5_000, response.VirtualLineCount);
    }
}
