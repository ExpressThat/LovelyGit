using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.CommitSearch;

public sealed partial class NativeCommitSearchReaderTests
{
    [Fact]
    public async Task SearchAsync_FindsSubjectAndBodyAndReturnsNewestFirst()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "2020-01-01T00:00:00Z", "First needle");
        await CommitAsync(
            repository,
            "2025-01-01T00:00:00Z",
            "Latest search result",
            "The detailed body contains the needle users need.");
        await RunGitAsync(repository, "branch", "search-marker");

        var response = await SearchAsync(repository, "needle", limit: 10);

        Assert.False(response.IsPartial);
        Assert.Equal(2, response.MatchingCommitCount);
        Assert.Equal("Latest search result", response.Results[0].Subject);
        Assert.Contains("needle", response.Results[0].Preview, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("search-marker", response.Results[0].Refs);
        Assert.Equal("First needle", response.Results[1].Subject);
    }

    [Fact]
    public async Task SearchAsync_MatchesAuthorEmailAndHashPrefix()
    {
        using var repository = TemporaryGitRepository.Create();
        await RunGitAsync(
            repository,
            "-c",
            "user.name=Alice Searcher",
            "-c",
            "user.email=alice.searcher@example.invalid",
            "commit",
            "--allow-empty",
            "-m",
            "Authored result");
        var hash = (await RunGitAsync(repository, "rev-parse", "HEAD")).Trim();

        var byAuthor = await SearchAsync(repository, "Alice Searcher", limit: 10);
        var byEmail = await SearchAsync(repository, "alice.searcher@", limit: 10);
        var byHash = await SearchAsync(repository, hash[..10], limit: 10);

        Assert.Equal(hash, Assert.Single(byAuthor.Results).Hash);
        Assert.Equal(hash, Assert.Single(byEmail.Results).Hash);
        Assert.Equal(hash, Assert.Single(byHash.Results).Hash);
    }

    [Fact]
    public async Task SearchAsync_ResolvesPackedSevenCharacterHashWithoutWalkingHistory()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "2025-01-01T00:00:00Z", "packed hash result");
        var hash = (await RunGitAsync(repository, "rev-parse", "HEAD")).Trim();
        await RunGitAsync(repository, "gc", "--prune=now");

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path,
            hash[..7],
            string.Empty,
            string.Empty,
            null,
            null,
            limit: 10,
            maximumCommits: 1,
            maximumDuration: TimeSpan.Zero,
            CancellationToken.None);

        Assert.Equal(hash, Assert.Single(response.Results).Hash);
        Assert.Equal(1, response.ScannedCommitCount);
        Assert.False(response.IsPartial);
    }

    [Fact]
    public async Task SearchAsync_BoundsResultsAndReportsPartialTraversal()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "2020-01-01T00:00:00Z", "bounded match one");
        await CommitAsync(repository, "2021-01-01T00:00:00Z", "bounded match two");
        await CommitAsync(repository, "2022-01-01T00:00:00Z", "bounded match three");

        var bounded = await SearchAsync(repository, "bounded match", limit: 1);
        var partial = await NativeCommitSearchReader.SearchAsync(
            repository.Path,
            "bounded match",
            string.Empty,
            string.Empty,
            null,
            null,
            limit: 10,
            maximumCommits: 1,
            maximumDuration: Timeout.InfiniteTimeSpan,
            CancellationToken.None);

        Assert.Equal(3, bounded.MatchingCommitCount);
        Assert.Equal("bounded match three", Assert.Single(bounded.Results).Subject);
        Assert.True(partial.IsPartial);
        Assert.Equal(1, partial.ScannedCommitCount);
    }

    [Fact]
    public async Task SearchAsync_IncludesHistoryReachableOnlyFromATag()
    {
        using var repository = TemporaryGitRepository.Create();
        var main = (await RunGitAsync(repository, "branch", "--show-current")).Trim();
        await RunGitAsync(repository, "switch", "--orphan", "tag-only-source");
        await RunGitAsync(repository, "commit", "--allow-empty", "-m", "tag-only needle");
        var taggedHash = (await RunGitAsync(repository, "rev-parse", "HEAD")).Trim();
        await RunGitAsync(repository, "tag", "search-tag-only");
        await RunGitAsync(repository, "switch", main);
        await RunGitAsync(repository, "branch", "-D", "tag-only-source");

        var response = await SearchAsync(repository, "tag-only needle", limit: 10);

        Assert.Equal(taggedHash, Assert.Single(response.Results).Hash);
        Assert.Contains("search-tag-only", response.Results[0].Refs);
    }

    [Fact]
    public async Task SearchAsync_PrioritizesHeadHistoryAheadOfManyTagTips()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "2024-01-01T00:00:00Z", "head-history needle");
        var main = (await RunGitAsync(repository, "branch", "--show-current")).Trim();
        for (var index = 0; index < 12; index++)
        {
            await RunGitAsync(repository, "switch", "--orphan", $"tag-source-{index}");
            await RunGitAsync(repository, "commit", "--allow-empty", "-m", $"tag history {index}");
            await RunGitAsync(repository, "tag", $"history-tag-{index}");
        }
        await RunGitAsync(repository, "switch", main);

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path,
            "head-history needle",
            string.Empty,
            string.Empty,
            null,
            null,
            limit: 10,
            maximumCommits: 2,
            maximumDuration: Timeout.InfiniteTimeSpan,
            CancellationToken.None);

        Assert.Equal("head-history needle", Assert.Single(response.Results).Subject);
        Assert.True(response.ScannedCommitCount <= 2);
    }

    [Fact]
    public async Task SearchAsync_ReturnsPartialResultsWhenDurationBudgetExpires()
    {
        using var repository = TemporaryGitRepository.Create();

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path,
            "initial",
            string.Empty,
            string.Empty,
            null,
            null,
            limit: 10,
            maximumCommits: 100,
            maximumDuration: TimeSpan.Zero,
            CancellationToken.None);

        Assert.True(response.IsPartial);
        Assert.Equal(0, response.ScannedCommitCount);
        Assert.Empty(response.Results);
    }

    private static Task<CommitSearchResponse> SearchAsync(
        TemporaryGitRepository repository,
        string query,
        int limit) =>
        NativeCommitSearchReader.SearchAsync(
            repository.Path,
            query,
            string.Empty,
            string.Empty,
            null,
            null,
            limit,
            maximumCommits: 100,
            maximumDuration: Timeout.InfiniteTimeSpan,
            CancellationToken.None);

    private static Task<string> CommitAsync(
        TemporaryGitRepository repository,
        string date,
        string subject,
        string? body = null)
    {
        var arguments = new List<string>
        {
            "commit",
            "--allow-empty",
            $"--date={date}",
            "-m",
            subject,
        };
        if (body != null)
        {
            arguments.Add("-m");
            arguments.Add(body);
        }

        return RunGitAsync(repository, arguments.ToArray());
    }

    private static Task<string> CommitAsAsync(
        TemporaryGitRepository repository,
        string author,
        string date,
        string subject) =>
        RunGitAsync(
            repository,
            "-c", $"user.name={author}",
            "-c", $"user.email={author.ToLowerInvariant()}@example.invalid",
            "commit", "--allow-empty", $"--date={date}", "-m", subject);

    private static async Task<string> RunGitAsync(
        TemporaryGitRepository repository,
        params string[] arguments)
    {
        var result = await new GitCliService().ExecuteBufferedAsync(
            arguments,
            repository.Path,
            cancellationToken: CancellationToken.None);
        return result.StandardOutput;
    }
}
