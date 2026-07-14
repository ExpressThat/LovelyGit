using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.CommitSearch;

public sealed partial class NativeCommitSearchReaderTests
{
    [Fact]
    public void DefaultSearchBudget_RemainsInteractive()
    {
        Assert.InRange(
            NativeCommitSearchReader.DefaultMaximumDuration,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(350));
    }

    [Fact]
    public async Task SearchAsync_ReturnsRecentMatchesAfterResponsiveScanWindow()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "2020-01-01T00:00:00Z", "older commit");
        await CommitAsync(repository, "2021-01-01T00:00:00Z", "responsive needle");
        await CommitAsync(repository, "2022-01-01T00:00:00Z", "newest commit");

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path,
            "responsive needle",
            string.Empty,
            string.Empty,
            null,
            null,
            limit: 10,
            maximumCommits: 100,
            maximumDuration: Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            responsiveMatchScanCount: 2);

        Assert.Equal(2, response.ScannedCommitCount);
        Assert.Equal("responsive needle", Assert.Single(response.Results).Subject);
        Assert.True(response.IsPartial);
    }

    [Fact]
    public async Task SearchAsync_DoesNotStopResponsiveWindowWithoutAMatch()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "2021-01-01T00:00:00Z", "first commit");
        await CommitAsync(repository, "2022-01-01T00:00:00Z", "second commit");

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path,
            "absent needle",
            string.Empty,
            string.Empty,
            null,
            null,
            limit: 10,
            maximumCommits: 100,
            maximumDuration: Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            responsiveMatchScanCount: 1);

        Assert.Equal(3, response.ScannedCommitCount);
        Assert.Empty(response.Results);
        Assert.False(response.IsPartial);
    }
}
