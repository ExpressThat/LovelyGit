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
            string.Empty,
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
            repository.Path, string.Empty, "Alice", string.Empty, null, null, 10, 100,
            Timeout.InfiniteTimeSpan, CancellationToken.None);

        Assert.Equal("alice result", Assert.Single(response.Results).Subject);
    }

    [Theory]
    [InlineData("topic")]
    [InlineData("origin/topic")]
    [InlineData("topic-tag")]
    [InlineData("refs/heads/topic")]
    public async Task SearchAsync_ScopesTraversalToLocalRemoteAndTagRefs(string scope)
    {
        using var repository = TemporaryGitRepository.Create();
        var main = (await RunGitAsync(repository, "branch", "--show-current")).Trim();
        await RunGitAsync(repository, "switch", "-c", "topic");
        await RunGitAsync(repository, "commit", "--allow-empty", "-m", "topic needle");
        var topicHash = (await RunGitAsync(repository, "rev-parse", "HEAD")).Trim();
        await RunGitAsync(repository, "tag", "topic-tag");
        await RunGitAsync(repository, "update-ref", "refs/remotes/origin/topic", topicHash);
        await RunGitAsync(repository, "switch", main);
        await RunGitAsync(repository, "commit", "--allow-empty", "-m", "main needle");

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path, "needle", string.Empty, scope, null, null, 10, 100,
            Timeout.InfiniteTimeSpan, CancellationToken.None);

        Assert.Equal("topic needle", Assert.Single(response.Results).Subject);
        Assert.Equal(scope, response.Scope);
    }

    [Fact]
    public async Task SearchAsync_RejectsMissingScopeWithoutFallingBackToAllHistory()
    {
        using var repository = TemporaryGitRepository.Create();

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            NativeCommitSearchReader.SearchAsync(
                repository.Path, string.Empty, string.Empty, "missing", null, null,
                10, 100, Timeout.InfiniteTimeSpan, CancellationToken.None));

        Assert.Contains("Git ref not found", error.Message);
    }
}
