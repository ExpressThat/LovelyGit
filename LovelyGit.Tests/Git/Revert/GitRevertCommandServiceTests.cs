using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Revert;

namespace LovelyGit.Tests.Git.Revert;

public sealed class GitRevertCommandServiceTests
{
    [Fact]
    public async Task RevertCommitAsync_RevertsSelectedCommit()
    {
        using var repository = TemporaryGitRepository.Create();
        var revertService = new GitRevertCommandService(repository.GitCliService);

        await revertService.RevertCommitAsync(
            repository.Path,
            repository.ChangeCommitHash,
            CancellationToken.None);

        var headSubject = await repository.GitCliService.ExecuteBufferedAsync(
            ["log", "-1", "--pretty=%s"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith("Revert \"Change file\"", headSubject.StandardOutput.Trim());
        Assert.Equal("before", File.ReadAllText(System.IO.Path.Combine(repository.Path, "file.txt")));
    }

    [Theory]
    [InlineData("not-a-hash", typeof(ArgumentException))]
    [InlineData("0000000000000000000000000000000000000000", typeof(GitOperationException))]
    public async Task RevertCommitAsync_InvalidTargetLeavesHistoryAndFileUnchanged(
        string commitHash,
        Type expectedException)
    {
        using var repository = TemporaryGitRepository.Create();
        var before = await repository.ReadHeadAsync();

        var exception = await Record.ExceptionAsync(() =>
            new GitRevertCommandService(repository.GitCliService).RevertCommitAsync(
                repository.Path,
                commitHash,
                CancellationToken.None));

        Assert.IsType(expectedException, exception);
        Assert.Equal(before, await repository.ReadHeadAsync());
        Assert.Equal("after", File.ReadAllText(Path.Combine(repository.Path, "file.txt")));
        Assert.False(File.Exists(Path.Combine(repository.Path, ".git", "REVERT_HEAD")));
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(
            DirectoryInfo directory,
            GitCliService gitCliService,
            string changeCommitHash)
        {
            _directory = directory;
            GitCliService = gitCliService;
            ChangeCommitHash = changeCommitHash;
            Path = directory.FullName;
        }

        public GitCliService GitCliService { get; }

        public string ChangeCommitHash { get; }

        public string Path { get; }

        public async Task<string> ReadHeadAsync()
        {
            var result = await GitCliService.ExecuteBufferedAsync(
                ["rev-parse", "HEAD"], Path, cancellationToken: CancellationToken.None);
            return result.StandardOutput.Trim();
        }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-revert-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "file.txt"), "before");
            RunGit(gitCliService, directory.FullName, ["add", "file.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "file.txt"), "after");
            RunGit(gitCliService, directory.FullName, ["commit", "-am", "Change file"]);

            var changeCommitHash = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();

            return new TemporaryGitRepository(directory, gitCliService, changeCommitHash);
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
