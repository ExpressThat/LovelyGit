using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Branches;

public sealed class GitBranchRemoteCommandServiceTests
{
    [Fact]
    public async Task PullBranchAsync_PullsOriginBranchIntoCurrentBranch()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);
        var localPath = repository.ClonePath;
        await repository.CommitAndPushFromUpdaterAsync("remote change");

        await branchService.PullBranchAsync(
            localPath,
            repository.DefaultBranchName,
            CancellationToken.None);

        var localHead = RunGit(
            repository.GitCliService,
            localPath,
            ["rev-parse", "HEAD"]).StandardOutput.Trim();

        Assert.Equal(repository.UpdaterHeadCommitHash, localHead);
    }

    [Fact]
    public async Task SetAndUnsetBranchUpstreamAsync_UpdatesBranchTracking()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);
        var upstreamName = $"origin/{repository.DefaultBranchName}";

        await branchService.SetBranchUpstreamAsync(
            repository.ClonePath,
            repository.DefaultBranchName,
            upstreamName,
            CancellationToken.None);

        var upstream = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["rev-parse", "--abbrev-ref", $"{repository.DefaultBranchName}@{{upstream}}"])
            .StandardOutput.Trim();

        var nativeUpstream = Assert.Single(
            await GitBranchUpstreamConfigReader.ReadAsync(
                Path.Combine(repository.ClonePath, ".git"),
                CancellationToken.None));

        await branchService.UnsetBranchUpstreamAsync(
            repository.ClonePath,
            repository.DefaultBranchName,
            CancellationToken.None);

        var missingUpstream = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["rev-parse", "--abbrev-ref", $"{repository.DefaultBranchName}@{{upstream}}"],
            validateExitCode: false);

        Assert.Equal(upstreamName, upstream);
        Assert.Equal(repository.DefaultBranchName, nativeUpstream.BranchName);
        Assert.Equal(upstreamName, nativeUpstream.UpstreamName);
        Assert.NotEqual(0, missingUpstream.ExitCode);
        Assert.Empty(
            await GitBranchUpstreamConfigReader.ReadAsync(
                Path.Combine(repository.ClonePath, ".git"),
                CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRemoteBranchAsync_RemovesOnlyTheRemoteBranch()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitBranchCommandService(repository.GitCliService);
        RunGit(repository.GitCliService, repository.ClonePath, ["switch", "-c", "feature/remove"]);
        RunGit(repository.GitCliService, repository.ClonePath, ["push", "origin", "feature/remove"]);
        RunGit(repository.GitCliService, repository.ClonePath, ["switch", repository.DefaultBranchName]);

        await service.DeleteRemoteBranchAsync(
            repository.ClonePath,
            "origin/feature/remove",
            CancellationToken.None);

        var remoteRef = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["ls-remote", "--heads", "origin", "refs/heads/feature/remove"]);
        var localRef = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["branch", "--list", "feature/remove"]);
        var localRemoteRef = RunGit(
            repository.GitCliService,
            repository.ClonePath,
            ["show-ref", "--verify", "refs/remotes/origin/feature/remove"],
            validateExitCode: false);
        Assert.Empty(remoteRef.StandardOutput);
        Assert.Contains("feature/remove", localRef.StandardOutput, StringComparison.Ordinal);
        Assert.NotEqual(0, localRemoteRef.ExitCode);
    }

    [Theory]
    [InlineData("origin")]
    [InlineData("bad remote/feature")]
    [InlineData("origin/../feature")]
    public async Task DeleteRemoteBranchAsync_RejectsInvalidNamesWithoutMutation(
        string remoteBranchName)
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitBranchCommandService(repository.GitCliService);
        var head = RunGit(
            repository.GitCliService,
            repository.Path,
            ["rev-parse", "HEAD"]).StandardOutput.Trim();

        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteRemoteBranchAsync(
            repository.Path,
            remoteBranchName,
            CancellationToken.None));

        var unchangedHead = RunGit(
            repository.GitCliService,
            repository.Path,
            ["rev-parse", "HEAD"]).StandardOutput.Trim();
        Assert.Equal(head, unchangedHead);
    }

    private sealed class TemporaryRemoteGitRepository : IDisposable
    {
        private static readonly RepositoryTemplate<(string Branch, string Root)> Template = new(
            "lovelygit-remote-pull-template-",
            InitializeTemplate);
        private readonly DirectoryInfo _directory;

        private TemporaryRemoteGitRepository(
            DirectoryInfo directory,
            GitCliService gitCliService,
            string defaultBranchName)
        {
            _directory = directory;
            GitCliService = gitCliService;
            DefaultBranchName = defaultBranchName;
            BarePath = Path.Combine(directory.FullName, "origin.git");
            ClonePath = Path.Combine(directory.FullName, "clone");
            UpdaterPath = Path.Combine(directory.FullName, "updater");
        }

        public string BarePath { get; }

        public string ClonePath { get; }

        public string DefaultBranchName { get; }

        public GitCliService GitCliService { get; }

        public string UpdaterHeadCommitHash { get; private set; } = string.Empty;

        private string UpdaterPath { get; }

        public async Task CommitAndPushFromUpdaterAsync(string message)
        {
            var filePath = Path.Combine(UpdaterPath, $"{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(filePath, message);
            RunGit(GitCliService, UpdaterPath, ["add", "."]);
            RunGit(GitCliService, UpdaterPath, ["commit", "-m", message]);
            UpdaterHeadCommitHash = RunGit(
                GitCliService,
                UpdaterPath,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();
            RunGit(GitCliService, UpdaterPath, ["push", "origin", DefaultBranchName]);
        }

        public static TemporaryRemoteGitRepository Create()
        {
            var (directory, state) = Template.CreateCopy("lovelygit-remote-pull-");
            var gitCliService = new GitCliService();
            var repository = new TemporaryRemoteGitRepository(
                directory,
                gitCliService,
                state.Branch);
            RemoteRepositoryTemplate.RetargetConfig(
                repository.ClonePath, state.Root, directory.FullName);
            RemoteRepositoryTemplate.RetargetConfig(
                repository.UpdaterPath, state.Root, directory.FullName);
            return repository;
        }

        private static (string Branch, string Root) InitializeTemplate(DirectoryInfo directory)
        {
            var gitCliService = new GitCliService();
            var repository = new TemporaryRemoteGitRepository(directory, gitCliService, "");

            RunGit(gitCliService, directory.FullName, ["init", "--bare", repository.BarePath]);
            RunGit(gitCliService, directory.FullName, ["init", repository.UpdaterPath]);
            ConfigureIdentity(gitCliService, repository.UpdaterPath);
            File.WriteAllText(Path.Combine(repository.UpdaterPath, "readme.txt"), "initial");
            RunGit(gitCliService, repository.UpdaterPath, ["add", "."]);
            RunGit(gitCliService, repository.UpdaterPath, ["commit", "-m", "initial"]);
            var defaultBranchName = RunGit(
                gitCliService,
                repository.UpdaterPath,
                ["branch", "--show-current"]).StandardOutput.Trim();
            RunGit(gitCliService, repository.UpdaterPath, ["remote", "add", "origin", repository.BarePath]);
            RunGit(gitCliService, repository.UpdaterPath, ["push", "origin", defaultBranchName]);
            RunGit(gitCliService, directory.FullName, ["clone", repository.BarePath, repository.ClonePath]);
            ConfigureIdentity(gitCliService, repository.ClonePath);

            return (defaultBranchName, directory.FullName);
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
        IReadOnlyList<string> arguments,
        bool validateExitCode = true)
    {
        return gitCliService
            .ExecuteBufferedAsync(arguments, workingDirectory, validateExitCode)
            .GetAwaiter()
            .GetResult();
    }
}
