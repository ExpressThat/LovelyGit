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
    public async Task CheckoutTagAsync_PreCancelledDoesNotMoveHeadOrChangeWorktree()
    {
        using var repository = TemporaryGitRepository.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var checkoutService = new GitCheckoutCommandService(repository.GitCliService);
        var originalHead = (await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"], repository.Path)).StandardOutput.Trim();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            checkoutService.CheckoutTagAsync(
                repository.Path,
                "v-test",
                cancellation.Token));

        var currentHead = (await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"], repository.Path)).StandardOutput.Trim();
        Assert.Equal(originalHead, currentHead);
        Assert.Equal("second\n", File.ReadAllText(Path.Combine(repository.Path, "tracked.txt")));
    }

    [Fact]
    public async Task CheckoutTagAsync_InvalidTagDoesNotRunGitMutation()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-tag-");
        var sentinel = Path.Combine(directory.FullName, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");

        try
        {
            var error = await Assert.ThrowsAsync<ArgumentException>(() =>
                new GitCheckoutCommandService(new GitCliService()).CheckoutTagAsync(
                    directory.FullName,
                    "not a tag",
                    CancellationToken.None));

            Assert.Contains("tag", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private static readonly RepositoryTemplate<string> Template = new(
            "lovelygit-checkout-template-",
            InitializeTemplate);
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
            var (directory, firstCommitHash) = Template.CreateCopy("lovelygit-checkout-");
            var gitCliService = new GitCliService();
            return new TemporaryGitRepository(directory, gitCliService, firstCommitHash);
        }

        private static string InitializeTemplate(DirectoryInfo directory)
        {
            var gitCliService = new GitCliService();
            InitializedRepositoryTemplate.CopyInto(directory, "master");
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

            return firstCommitHash;
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
