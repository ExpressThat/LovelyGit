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
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-checkout-commit-");
        var sentinel = Path.Combine(directory.FullName, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");

        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                new GitBranchCommandService(new GitCliService()).CheckoutCommitAsync(
                    directory.FullName,
                    hash,
                    CancellationToken.None));

            Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
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
        var head = await OutputAsync(repository, "rev-parse", "HEAD");
        var status = await OutputAsync(repository, "status", "--short");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CheckoutCommitAsync(
                repository.Path,
                repository.HeadCommitHash,
                cancellation.Token));

        Assert.NotEqual(string.Empty, await OutputAsync(repository, "branch", "--show-current"));
        Assert.Equal(head, await OutputAsync(repository, "rev-parse", "HEAD"));
        Assert.Equal(status, await OutputAsync(repository, "status", "--short"));
    }

    [Fact]
    public async Task CheckoutCommitAsync_MissingCommitDoesNotMoveHead()
    {
        using var repository = TemporaryGitRepository.Create();
        var head = await OutputAsync(repository, "rev-parse", "HEAD");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GitBranchCommandService(repository.GitCliService).CheckoutCommitAsync(
                repository.Path, new string('f', 40), CancellationToken.None));

        Assert.Equal(head, await OutputAsync(repository, "rev-parse", "HEAD"));
        Assert.NotEqual(string.Empty, await OutputAsync(repository, "branch", "--show-current"));
    }

    [Fact]
    public async Task CheckoutCommitAsync_NonCommitObjectDoesNotMoveHead()
    {
        using var repository = TemporaryGitRepository.Create();
        var head = await OutputAsync(repository, "rev-parse", "HEAD");
        var blobPath = Path.Combine(repository.Path, "blob.txt");
        await File.WriteAllTextAsync(blobPath, "not a commit");
        var blob = await OutputAsync(repository, "hash-object", "-w", "--", blobPath);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new GitBranchCommandService(repository.GitCliService).CheckoutCommitAsync(
                repository.Path, blob, CancellationToken.None));

        Assert.Equal(head, await OutputAsync(repository, "rev-parse", "HEAD"));
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
