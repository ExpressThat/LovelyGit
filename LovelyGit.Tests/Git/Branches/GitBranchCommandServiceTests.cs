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

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(
            DirectoryInfo directory,
            GitCliService gitCliService,
            string headCommitHash)
        {
            _directory = directory;
            GitCliService = gitCliService;
            HeadCommitHash = headCommitHash;
            Path = directory.FullName;
        }

        public GitCliService GitCliService { get; }

        public string HeadCommitHash { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-branch-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "Initial"]);

            var headCommitHash = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();

            return new TemporaryGitRepository(directory, gitCliService, headCommitHash);
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
