using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.CommitSearch;

public sealed partial class NativeCommitSearchReaderTests
{
    [Fact]
    public async Task SearchAsync_CombinesAuthorAndInclusiveCalendarDateFilters()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsAsync(repository, "Alice", "2024-01-01T12:00:00Z", "older match");
        await CommitAsAsync(repository, "Bob", "2024-06-15T12:00:00Z", "other author");
        await CommitAsAsync(repository, "Alice", "2024-06-30T23:30:00Z", "wanted match");
        await CommitAsAsync(repository, "Alice", "2024-07-01T00:00:00Z", "too new");

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path,
            "match",
            "alice",
            new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
            10, 100, Timeout.InfiniteTimeSpan, CancellationToken.None);

        var result = Assert.Single(response.Results);
        Assert.Equal("wanted match", result.Subject);
        Assert.Equal("alice", response.Author);
        Assert.Equal(1, response.MatchingCommitCount);
    }

    [Fact]
    public async Task SearchAsync_SupportsFilterOnlySearchWithoutEmptyHashMatching()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsAsync(repository, "Alice", "2024-01-01T00:00:00Z", "alice result");
        await CommitAsAsync(repository, "Bob", "2024-02-01T00:00:00Z", "bob result");

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path, string.Empty, "Alice", null, null, 10, 100,
            Timeout.InfiniteTimeSpan, CancellationToken.None);

        Assert.Equal("alice result", Assert.Single(response.Results).Subject);
    }
}
