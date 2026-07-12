using ExpressThat.LovelyGit.Services.Git.CommitGraph;

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
}
