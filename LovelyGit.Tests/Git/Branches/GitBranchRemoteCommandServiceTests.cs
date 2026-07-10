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

    private sealed class TemporaryRemoteGitRepository : IDisposable
    {
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
            var directory = Directory.CreateTempSubdirectory("lovelygit-remote-pull-");
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

            return new TemporaryRemoteGitRepository(directory, gitCliService, defaultBranchName);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }
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
