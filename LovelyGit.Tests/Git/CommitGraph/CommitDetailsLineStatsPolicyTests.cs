using ExpressThat.LovelyGit.Services.Git.CommitGraph;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitDetailsLineStatsPolicyTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(500, true)]
    [InlineData(501, false)]
    [InlineData(int.MaxValue, false)]
    public void ShouldCalculate_UsesBoundedLargeCommitPolicy(int fileCount, bool expected)
    {
        Assert.Equal(expected, CommitDetailsLineStatsPolicy.ShouldCalculate(fileCount));
    }
}
