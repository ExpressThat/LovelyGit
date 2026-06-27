using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Rebase;

namespace LovelyGit.Tests.Git.Rebase;

public sealed class GitRebaseCommandServiceTests
{
    [Fact]
    public async Task RebaseCurrentBranchOntoBranchAsync_RebasesCurrentBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var rebaseService = new GitRebaseCommandService(repository.GitCliService);

        await rebaseService.RebaseCurrentBranchOntoBranchAsync(
            repository.Path,
            "feature/base",
            CancellationToken.None);

        Assert.Equal("current change", File.ReadAllText(repository.CurrentFilePath));
        var baseIsAncestor = await repository.GitCliService.ExecuteBufferedAsync(
            ["merge-base", "--is-ancestor", repository.BaseCommitHash, "HEAD"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, baseIsAncestor.ExitCode);
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(DirectoryInfo directory, GitCliService gitCliService, string baseCommitHash)
        {
            _directory = directory;
            GitCliService = gitCliService;
            BaseCommitHash = baseCommitHash;
            Path = directory.FullName;
            CurrentFilePath = System.IO.Path.Combine(Path, "current.txt");
        }

        public GitCliService GitCliService { get; }

        public string BaseCommitHash { get; }

        public string CurrentFilePath { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-rebase-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "Initial"]);
            var defaultBranchName = RunGit(
                gitCliService,
                directory.FullName,
                ["branch", "--show-current"]).StandardOutput.Trim();
            RunGit(gitCliService, directory.FullName, ["checkout", "-b", "feature/base"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "base.txt"), "base change");
            RunGit(gitCliService, directory.FullName, ["add", "base.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Base"]);
            var baseCommitHash = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();
            RunGit(gitCliService, directory.FullName, ["checkout", defaultBranchName]);
            RunGit(gitCliService, directory.FullName, ["checkout", "-b", "feature/current"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "current.txt"), "current change");
            RunGit(gitCliService, directory.FullName, ["add", "current.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Current"]);

            return new TemporaryGitRepository(directory, gitCliService, baseCommitHash);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        private static BufferedCommandResult RunGit(
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
