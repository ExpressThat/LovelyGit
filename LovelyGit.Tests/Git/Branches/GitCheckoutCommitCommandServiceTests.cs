using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Branches;

public sealed class GitCheckoutCommitCommandServiceTests
{
    [Fact]
    public async Task CheckoutCommitAsync_DetachesHeadAtExactCommit()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitBranchCommandService(repository.GitCliService);

        await service.CheckoutCommitAsync(
            repository.Path,
            repository.HeadCommitHash,
            CancellationToken.None);

        Assert.Equal(string.Empty, await OutputAsync(repository, "branch", "--show-current"));
        Assert.Equal(repository.HeadCommitHash, await OutputAsync(repository, "rev-parse", "HEAD"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("0123456")]
    public async Task CheckoutCommitAsync_RejectsInvalidHashWithoutMovingHead(string hash)
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitBranchCommandService(repository.GitCliService);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CheckoutCommitAsync(
            repository.Path,
            hash,
            CancellationToken.None));

        Assert.Equal(repository.HeadCommitHash, await OutputAsync(repository, "rev-parse", "HEAD"));
        Assert.NotEqual(string.Empty, await OutputAsync(repository, "branch", "--show-current"));
    }

    [Fact]
    public async Task CheckoutCommitAsync_ConflictingChangesPreserveBranchAndWorktree()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitBranchCommandService(repository.GitCliService);
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "tracked.txt"), "committed");
        await RunAsync(repository, "add", "tracked.txt");
        await RunAsync(repository, "commit", "-m", "Tracked file");
        var branchHead = await OutputAsync(repository, "rev-parse", "HEAD");
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "tracked.txt"), "local change");

        var error = await Assert.ThrowsAsync<GitOperationException>(() =>
            service.CheckoutCommitAsync(
                repository.Path,
                repository.HeadCommitHash,
                CancellationToken.None));

        Assert.Contains("local changes", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(branchHead, await OutputAsync(repository, "rev-parse", "HEAD"));
        Assert.Equal("local change", await File.ReadAllTextAsync(
            Path.Combine(repository.Path, "tracked.txt")));
    }

    [Fact]
    public async Task CheckoutCommitAsync_CancellationDoesNotMoveHead()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitBranchCommandService(repository.GitCliService);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CheckoutCommitAsync(
                repository.Path,
                repository.HeadCommitHash,
                cancellation.Token));

        Assert.NotEqual(string.Empty, await OutputAsync(repository, "branch", "--show-current"));
    }

    private static async Task RunAsync(
        TemporaryGitRepository repository,
        params string[] arguments) =>
        await repository.GitCliService.ExecuteBufferedAsync(arguments, repository.Path);

    private static async Task<string> OutputAsync(
        TemporaryGitRepository repository,
        params string[] arguments) =>
        (await repository.GitCliService.ExecuteBufferedAsync(arguments, repository.Path))
        .StandardOutput.Trim();
}
