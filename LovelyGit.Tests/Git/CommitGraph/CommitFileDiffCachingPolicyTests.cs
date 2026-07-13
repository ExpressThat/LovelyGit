using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitFileDiffCachingPolicyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanUsePersistentCache_AllowsDefaultParentVariants(bool ignoreWhitespace)
    {
        Assert.True(CommitFileDiffCachingPolicy.CanUsePersistentCache(0, ignoreWhitespace));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CanUsePersistentCache_RejectsNonDefaultParents(int parentIndex)
    {
        Assert.False(CommitFileDiffCachingPolicy.CanUsePersistentCache(parentIndex, ignoreWhitespace: false));
    }

    [Fact]
    public void ShouldPersist_AllowsSmallRenderedDiffs()
    {
        var response = new CommitFileDiffResponse
        {
            Lines = [new CommitFileDiffLine { OldText = "before", NewText = "after" }],
        };

        Assert.True(CommitFileDiffCachingPolicy.ShouldPersist(response));
    }

    [Theory]
    [InlineData("compact")]
    [InlineData("virtual")]
    [InlineData("expanded")]
    public void ShouldPersist_RejectsPayloadsBeyondTheMemoryBudget(string kind)
    {
        var oversized = new string(
            'x',
            CommitFileDiffCachingPolicy.MaximumPersistentPayloadCharacters + 1);
        var response = kind switch
        {
            "compact" => new CommitFileDiffResponse { CompactLinesGzipBase64 = oversized },
            "virtual" => new CommitFileDiffResponse { VirtualTextGzipBase64 = oversized },
            _ => new CommitFileDiffResponse
            {
                Lines = [new CommitFileDiffLine { Text = oversized }],
            },
        };

        Assert.False(CommitFileDiffCachingPolicy.ShouldPersist(response));
    }
}
