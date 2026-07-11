using ExpressThat.LovelyGit.Services.Git.Checkout;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Checkout;

public sealed class GitCheckoutCommandServiceTests
{
    [Fact]
    public async Task CheckoutCommitDetachedAsync_DetachesHeadAtCommit()
    {
        using var repository = TemporaryGitRepository.Create();
        var checkoutService = new GitCheckoutCommandService(repository.GitCliService);

        await checkoutService.CheckoutCommitDetachedAsync(
            repository.Path,
            repository.FirstCommitHash,
            CancellationToken.None);

        var head = await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        var branch = await repository.GitCliService.ExecuteBufferedAsync(
            ["branch", "--show-current"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.Equal(repository.FirstCommitHash, head.StandardOutput.Trim());
        Assert.Equal(string.Empty, branch.StandardOutput.Trim());
    }

    [Fact]
    public async Task CheckoutBranchAsync_SwitchesToLocalBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var checkoutService = new GitCheckoutCommandService(repository.GitCliService);

        await checkoutService.CheckoutBranchAsync(
            repository.Path,
            "feature/test-branch",
            CancellationToken.None);

        var branch = await repository.GitCliService.ExecuteBufferedAsync(
            ["branch", "--show-current"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.Equal("feature/test-branch", branch.StandardOutput.Trim());
    }

    [Fact]
    public async Task CheckoutTagAsync_DetachesHeadAtTag()
    {
        using var repository = TemporaryGitRepository.Create();
        var checkoutService = new GitCheckoutCommandService(repository.GitCliService);

        await checkoutService.CheckoutTagAsync(
            repository.Path,
            "v-test",
            CancellationToken.None);

        var head = await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        var branch = await repository.GitCliService.ExecuteBufferedAsync(
            ["branch", "--show-current"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.Equal(repository.FirstCommitHash, head.StandardOutput.Trim());
        Assert.Equal(string.Empty, branch.StandardOutput.Trim());
    }

    [Fact]
    public async Task CheckoutTagAsync_DirtyOverwriteFailsWithoutMovingHead()
    {
        using var repository = TemporaryGitRepository.Create();
        var checkoutService = new GitCheckoutCommandService(repository.GitCliService);
        var originalHead = (await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            repository.Path)).StandardOutput.Trim();
        File.WriteAllText(Path.Combine(repository.Path, "tracked.txt"), "unsaved\n");

        var exception = await Assert.ThrowsAsync<GitOperationException>(() =>
            checkoutService.CheckoutTagAsync(
                repository.Path,
                "v-test",
                CancellationToken.None));

        Assert.Contains("working", exception.Message, StringComparison.OrdinalIgnoreCase);
        var currentHead = (await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            repository.Path)).StandardOutput.Trim();
        Assert.Equal(originalHead, currentHead);
        Assert.Equal("unsaved\n", File.ReadAllText(Path.Combine(repository.Path, "tracked.txt")));
    }

    [Fact]
    public async Task CheckoutTagAsync_InvalidTagDoesNotRunGitMutation()
    {
        using var repository = TemporaryGitRepository.Create();
        var checkoutService = new GitCheckoutCommandService(repository.GitCliService);
        var originalBranch = (await repository.GitCliService.ExecuteBufferedAsync(
            ["branch", "--show-current"],
            repository.Path)).StandardOutput.Trim();

        await Assert.ThrowsAsync<ArgumentException>(() => checkoutService.CheckoutTagAsync(
            repository.Path,
            "not a tag",
            CancellationToken.None));

        var branch = (await repository.GitCliService.ExecuteBufferedAsync(
            ["branch", "--show-current"],
            repository.Path)).StandardOutput.Trim();
        Assert.Equal(originalBranch, branch);
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(
            DirectoryInfo directory,
            GitCliService gitCliService,
            string firstCommitHash)
        {
            _directory = directory;
            GitCliService = gitCliService;
            FirstCommitHash = firstCommitHash;
            Path = directory.FullName;
        }

        public GitCliService GitCliService { get; }

        public string FirstCommitHash { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-checkout-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "first\n");
            RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "First"]);
            var firstCommitHash = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "second\n");
            RunGit(gitCliService, directory.FullName, ["commit", "-am", "Second"]);
            RunGit(gitCliService, directory.FullName, ["branch", "feature/test-branch", firstCommitHash]);
            RunGit(gitCliService, directory.FullName, ["tag", "v-test", firstCommitHash]);

            return new TemporaryGitRepository(directory, gitCliService, firstCommitHash);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        private static CliWrap.Buffered.BufferedCommandResult RunGit(
            GitCliService gitCliService,
            string workingDirectory,
            IReadOnlyList<string> arguments)
        {
            return gitCliService
                .ExecuteBufferedAsync(arguments, workingDirectory)
                .GetAwaiter()
                .GetResult();
        }
    }
}
