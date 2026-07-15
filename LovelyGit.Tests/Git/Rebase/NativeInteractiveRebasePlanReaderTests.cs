using ExpressThat.LovelyGit.Services.Git.Cli;
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

    [Fact]
    public async Task ReadAsync_RejectsDetachedHeadAndEmptyRange()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "Change");
        var head = await HeadAsync(repository);

        var empty = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NativeInteractiveRebasePlanReader.ReadAsync(
                repository.Path, head, CancellationToken.None));
        Assert.Contains("before HEAD", empty.Message);

        await RunAsync(repository, "checkout", "--detach");
        var detached = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NativeInteractiveRebasePlanReader.ReadAsync(
                repository.Path, repository.HeadCommitHash, CancellationToken.None));
        Assert.Contains("Check out a branch", detached.Message);
    }

    [Fact]
    public async Task ReadAsync_RejectsInvalidHashWithoutChangingHead()
    {
        using var repository = TemporaryGitRepository.Create();
        var head = await HeadAsync(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            NativeInteractiveRebasePlanReader.ReadAsync(
                repository.Path, "not-an-object-id", CancellationToken.None));

        Assert.Equal(head, await HeadAsync(repository));
    }

    [Fact]
    public async Task ReadAsync_WhenCancelled_DoesNotChangeRepository()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitAsync(repository, "Change");
        var head = await HeadAsync(repository);
        var status = await RunAsync(repository, "status", "--short");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            NativeInteractiveRebasePlanReader.ReadAsync(
                repository.Path, repository.HeadCommitHash, cancellation.Token));

        Assert.Equal(head, await HeadAsync(repository));
        Assert.Equal(status, await RunAsync(repository, "status", "--short"));
    }

    [Fact]
    public async Task ReadAsync_RejectsRepositoryWithoutHeadCommit()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-rebase-unborn-");
        try
        {
            await new GitCliService().ExecuteBufferedAsync(["init"], directory.FullName);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                NativeInteractiveRebasePlanReader.ReadAsync(
                    directory.FullName, new string('0', 40), CancellationToken.None));

            Assert.Contains("does not have a HEAD", exception.Message);
        }
        finally
        {
            foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            directory.Delete(recursive: true);
        }
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
