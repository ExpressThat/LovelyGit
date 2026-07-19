using ExpressThat.LovelyGit.Services.Git.Checkout;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Checkout;

public sealed class GitCheckoutRemoteCommandServiceTests
{
    [Fact]
    public async Task CheckoutRemoteBranchAsync_CreatesLocalTrackingBranch()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var checkoutService = new GitCheckoutCommandService(repository.GitCliService);

        await checkoutService.CheckoutRemoteBranchAsync(
            repository.ClonePath,
            "origin/feature/remote",
            "feature/local",
            CancellationToken.None);

        var currentBranch = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["branch", "--show-current"]).StandardOutput.Trim();
        var upstream = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["rev-parse", "--abbrev-ref", "feature/local@{upstream}"])
            .StandardOutput.Trim();

        Assert.Equal("feature/local", currentBranch);
        Assert.Equal("origin/feature/remote", upstream);
    }

    [Fact]
    public async Task CheckoutRemoteBranchAsync_DuplicateLocalNameLeavesHeadUnchanged()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var checkoutService = new GitCheckoutCommandService(repository.GitCliService);
        RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["switch", "--create", "existing-local", "--track", "origin/feature/remote"]);
        var originalHead = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["rev-parse", "HEAD"]).StandardOutput.Trim();

        await Assert.ThrowsAsync<GitOperationException>(() =>
            checkoutService.CheckoutRemoteBranchAsync(
                repository.ClonePath,
                "origin/feature/remote",
                "existing-local",
                CancellationToken.None));

        var currentHead = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["rev-parse", "HEAD"]).StandardOutput.Trim();
        Assert.Equal(originalHead, currentHead);
    }

    [Theory]
    [InlineData("origin")]
    [InlineData("bad remote/feature")]
    [InlineData("origin/../feature")]
    public async Task CheckoutRemoteBranchAsync_InvalidRemoteNameCreatesNoLocalBranch(
        string remoteBranchName)
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var checkoutService = new GitCheckoutCommandService(repository.GitCliService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            checkoutService.CheckoutRemoteBranchAsync(
                repository.ClonePath,
                remoteBranchName,
                "local-copy",
                CancellationToken.None));

        var localBranch = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["branch", "--list", "local-copy"]);
        Assert.Empty(localBranch.StandardOutput);
    }

    private sealed class TemporaryRemoteGitRepository : IDisposable
    {
        private static readonly RepositoryTemplate<string> Template = new(
            "lovelygit-remote-checkout-template-",
            InitializeTemplate);
        private readonly DirectoryInfo _directory;

        private TemporaryRemoteGitRepository(DirectoryInfo directory)
        {
            _directory = directory;
            GitCliService = new GitCliService();
            BarePath = Path.Combine(directory.FullName, "origin.git");
            ClonePath = Path.Combine(directory.FullName, "clone");
            UpdaterPath = Path.Combine(directory.FullName, "updater");
        }

        public string BarePath { get; }

        public string ClonePath { get; }

        public GitCliService GitCliService { get; }

        private string UpdaterPath { get; }

        public static TemporaryRemoteGitRepository Create()
        {
            var (directory, templateRoot) = Template.CreateCopy("lovelygit-remote-checkout-");
            var repository = new TemporaryRemoteGitRepository(directory);
            RemoteRepositoryTemplate.RetargetConfig(
                repository.UpdaterPath, templateRoot, directory.FullName);
            RemoteRepositoryTemplate.RetargetConfig(
                repository.ClonePath, templateRoot, directory.FullName);

            return repository;
        }

        private static string InitializeTemplate(DirectoryInfo directory)
        {
            var repository = new TemporaryRemoteGitRepository(directory);
            var gitCliService = repository.GitCliService;

            RunGit(gitCliService, directory.FullName, ["init", "--bare", repository.BarePath]);
            RunGit(gitCliService, directory.FullName, ["init", repository.UpdaterPath]);
            ConfigureIdentity(gitCliService, repository.UpdaterPath);
            RunGit(gitCliService, repository.UpdaterPath, ["commit", "--allow-empty", "-m", "initial"]);
            RunGit(gitCliService, repository.UpdaterPath, ["checkout", "-b", "feature/remote"]);
            RunGit(gitCliService, repository.UpdaterPath, ["commit", "--allow-empty", "-m", "remote"]);
            RunGit(gitCliService, repository.UpdaterPath, ["remote", "add", "origin", repository.BarePath]);
            RunGit(gitCliService, repository.UpdaterPath, ["push", "origin", "feature/remote"]);
            RunGit(gitCliService, directory.FullName, ["clone", repository.BarePath, repository.ClonePath]);
            ConfigureIdentity(gitCliService, repository.ClonePath);

            return directory.FullName;
        }

        public void Dispose() => RepositoryTemplateLifetime.DeleteDirectory(_directory);
    }

    private static void ConfigureIdentity(GitCliService gitCliService, string path)
    {
        RunGit(gitCliService, path, ["config", "user.name", "LovelyGit Test"]);
        RunGit(gitCliService, path, ["config", "user.email", "test@example.invalid"]);
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
