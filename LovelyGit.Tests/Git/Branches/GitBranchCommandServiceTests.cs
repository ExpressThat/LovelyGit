using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Branches;

public sealed class GitBranchCommandServiceTests
{
    [Fact]
    public async Task CreateBranchAsync_CreatesBranchAtCommit()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);

        await branchService.CreateBranchAsync(
            repository.Path,
            "feature/from-commit",
            repository.HeadCommitHash,
            CancellationToken.None);

        var branchRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/from-commit"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, branchRef.StandardOutput);
    }

    [Fact]
    public async Task CreateBranchFromTagAsync_CreatesBranchAtTag()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["tag", "v-test-branch-source", repository.HeadCommitHash],
            repository.Path,
            cancellationToken: CancellationToken.None);

        await branchService.CreateBranchFromTagAsync(
            repository.Path,
            "feature/from-tag",
            "v-test-branch-source",
            CancellationToken.None);

        var branchRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/from-tag"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, branchRef.StandardOutput);
    }

    [Fact]
    public async Task RenameBranchAsync_RenamesLocalBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);

        await branchService.CreateBranchAsync(
            repository.Path,
            "feature/old-name",
            repository.HeadCommitHash,
            CancellationToken.None);

        await branchService.RenameBranchAsync(
            repository.Path,
            "feature/old-name",
            "feature/new-name",
            CancellationToken.None);

        var renamedRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/new-name"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        var oldRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/old-name"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, renamedRef.StandardOutput);
        Assert.NotEqual(0, oldRef.ExitCode);
    }

    [Fact]
    public async Task DeleteBranchAsync_DeletesMergedBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);
        await branchService.CreateBranchAsync(
            repository.Path,
            "feature/delete-me",
            repository.HeadCommitHash,
            CancellationToken.None);

        await branchService.DeleteBranchAsync(
            repository.Path,
            "feature/delete-me",
            force: false,
            CancellationToken.None);

        var branchRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/delete-me"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(0, branchRef.ExitCode);
    }

    [Fact]
    public async Task DeleteBranchAsync_ForceDeletesUnmergedBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);
        await repository.CreateUnmergedBranchAsync("feature/force-delete-me");

        await branchService.DeleteBranchAsync(
            repository.Path,
            "feature/force-delete-me",
            force: true,
            CancellationToken.None);

        var branchRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/force-delete-me"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(0, branchRef.ExitCode);
    }

    [Fact]
    public async Task PushBranchAsync_PushesBranchToOrigin()
    {
        using var repository = TemporaryGitRepository.Create();
        using var remote = TemporaryBareGitRepository.Create(repository.GitCliService);
        var branchService = new GitBranchCommandService(repository.GitCliService);
        await branchService.CreateBranchAsync(
            repository.Path,
            "feature/push-me",
            repository.HeadCommitHash,
            CancellationToken.None);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["remote", "add", "origin", remote.Path],
            repository.Path,
            cancellationToken: CancellationToken.None);

        await branchService.PushBranchAsync(
            repository.Path,
            "feature/push-me",
            CancellationToken.None);

        var remoteRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/push-me"],
            remote.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, remoteRef.StandardOutput);
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(
            DirectoryInfo directory,
            GitCliService gitCliService,
            string defaultBranchName,
            string headCommitHash)
        {
            _directory = directory;
            DefaultBranchName = defaultBranchName;
            GitCliService = gitCliService;
            HeadCommitHash = headCommitHash;
            Path = directory.FullName;
        }

        private string DefaultBranchName { get; }

        public GitCliService GitCliService { get; }

        public string HeadCommitHash { get; }

        public string Path { get; }

        public async Task CreateUnmergedBranchAsync(string branchName)
        {
            RunGit(GitCliService, Path, ["checkout", "-b", branchName]);
            RunGit(GitCliService, Path, ["commit", "--allow-empty", "-m", "Unmerged"]);
            await GitCliService.ExecuteBufferedAsync(
                ["checkout", DefaultBranchName],
                Path,
                cancellationToken: CancellationToken.None);
        }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-branch-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "Initial"]);
            var defaultBranchName = RunGit(
                gitCliService,
                directory.FullName,
                ["branch", "--show-current"]).StandardOutput.Trim();

            var headCommitHash = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();

            return new TemporaryGitRepository(directory, gitCliService, defaultBranchName, headCommitHash);
        }

        public void Dispose()
        {
            DeleteDirectory(_directory);
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

    private sealed class TemporaryBareGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryBareGitRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryBareGitRepository Create(GitCliService gitCliService)
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-remote-");
            gitCliService.ExecuteBufferedAsync(["init", "--bare"], directory.FullName)
                .GetAwaiter()
                .GetResult();
            return new TemporaryBareGitRepository(directory);
        }

        public void Dispose()
        {
            DeleteDirectory(_directory);
        }
    }

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        directory.Delete(recursive: true);
    }
}
