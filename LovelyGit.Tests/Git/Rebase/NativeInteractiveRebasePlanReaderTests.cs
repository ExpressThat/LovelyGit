using ExpressThat.LovelyGit.Services.Git.Rebase;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.Rebase;

public sealed class NativeInteractiveRebasePlanReaderTests
{
    [Fact]
    public async Task ReadAsync_ReturnsOldestFirstCommitsAfterSelectedBase()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "First change");
        var firstHash = await HeadAsync(repository);
        await CommitAsync(repository, "Second change");

        var plan = await NativeInteractiveRebasePlanReader.ReadAsync(
            repository.Path,
            repository.HeadCommitHash,
            CancellationToken.None);

        Assert.Equal(repository.HeadCommitHash, plan.BaseCommitHash);
        Assert.False(string.IsNullOrWhiteSpace(plan.CurrentBranchName));
        Assert.Equal(["First change", "Second change"], plan.Commits.Select(commit => commit.Subject));
        Assert.Equal(firstHash, plan.Commits[0].Hash);
        Assert.Equal("LovelyGit Test", plan.Commits[0].AuthorName);
    }

    [Fact]
    public async Task ReadAsync_RejectsBaseOutsideFirstParentHistory()
    {
        using var repository = TemporaryGitRepository.Create();
        await RunAsync(repository, "checkout", "-b", "side");
        await CommitAsync(repository, "Side commit");
        var sideHash = await HeadAsync(repository);
        await RunAsync(repository, "checkout", "-");
        await CommitAsync(repository, "Main commit");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NativeInteractiveRebasePlanReader.ReadAsync(
                repository.Path,
                sideHash,
                CancellationToken.None));

        Assert.Contains("not an ancestor", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAsync_RejectsRangesContainingMergeCommits()
    {
        using var repository = TemporaryGitRepository.Create();
        await RunAsync(repository, "checkout", "-b", "side");
        await CommitAsync(repository, "Side commit");
        await RunAsync(repository, "checkout", "-");
        await CommitAsync(repository, "Main commit");
        await RunAsync(repository, "merge", "--no-ff", "--no-edit", "side");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NativeInteractiveRebasePlanReader.ReadAsync(
                repository.Path,
                repository.HeadCommitHash,
                CancellationToken.None));

        Assert.Contains("merge commit", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Task CommitAsync(TemporaryGitRepository repository, string subject) =>
        RunAsync(repository, "commit", "--allow-empty", "-m", subject);

    private static async Task<string> HeadAsync(TemporaryGitRepository repository) =>
        (await RunAsync(repository, "rev-parse", "HEAD")).Trim();

    private static async Task<string> RunAsync(
        TemporaryGitRepository repository,
        params string[] arguments)
    {
        var result = await repository.GitCliService.ExecuteBufferedAsync(
            arguments,
            repository.Path,
            cancellationToken: CancellationToken.None);
        return result.StandardOutput;
    }
}
