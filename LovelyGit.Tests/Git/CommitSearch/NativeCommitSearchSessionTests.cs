using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.CommitSearch;

public sealed class NativeCommitSearchSessionTests
{
    [Fact]
    public async Task ScanAsync_ContinuesFromPreviousFrontier()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "first commit");
        await CommitAsync(repository, "continuation needle");
        await CommitAsync(repository, "newest commit");
        using var session = await OpenAsync(repository, "continuation needle");

        var initial = await session.ScanAsync(
            2, Timeout.InfiniteTimeSpan, CancellationToken.None);
        var completed = await session.ScanAsync(
            100, Timeout.InfiniteTimeSpan, CancellationToken.None);

        Assert.Equal(2, initial.ScannedCommitCount);
        Assert.True(initial.IsPartial);
        Assert.Equal(4, completed.ScannedCommitCount);
        Assert.Equal("continuation needle", Assert.Single(completed.Results).Subject);
        Assert.False(completed.IsPartial);
    }

    [Fact]
    public async Task ScanAsync_CancellationPreservesFrontierForRetry()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "retry needle");
        using var session = await OpenAsync(repository, "retry needle");
        var initial = await session.ScanAsync(
            1, Timeout.InfiniteTimeSpan, CancellationToken.None);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => session.ScanAsync(
            100, Timeout.InfiniteTimeSpan, source.Token));
        var retry = await session.ScanAsync(
            100, Timeout.InfiniteTimeSpan, CancellationToken.None);

        Assert.Equal(1, initial.ScannedCommitCount);
        Assert.Equal(2, retry.ScannedCommitCount);
        Assert.Single(retry.Results);
        Assert.False(retry.IsPartial);
    }

    [Fact]
    public async Task Matches_RejectsChangedRepositoryRefs()
    {
        using var repository = TemporaryGitRepository.Create();
        using var session = await OpenAsync(repository, "initial");
        await CommitAsync(repository, "repository changed");

        Assert.False(session.Matches(
            repository.Path, "initial", string.Empty, string.Empty, null, null, 10));
    }

    private static Task<NativeCommitSearchSession> OpenAsync(
        TemporaryGitRepository repository,
        string query) =>
        NativeCommitSearchSession.OpenAsync(
            repository.Path,
            query,
            string.Empty,
            string.Empty,
            null,
            null,
            10,
            CancellationToken.None);

    private static async Task CommitAsync(TemporaryGitRepository repository, string subject)
    {
        await new GitCliService().ExecuteBufferedAsync(
            ["commit", "--allow-empty", "-m", subject],
            repository.Path,
            cancellationToken: CancellationToken.None);
    }
}
