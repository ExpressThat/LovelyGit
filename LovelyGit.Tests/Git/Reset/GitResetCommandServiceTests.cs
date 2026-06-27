using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Reset;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;

namespace LovelyGit.Tests.Git.Reset;

public sealed class GitResetCommandServiceTests
{
    [Fact]
    public async Task ResetCurrentBranchToCommitAsync_SoftResetKeepsChangeStaged()
    {
        using var repository = TemporaryGitRepository.Create();
        var resetService = new GitResetCommandService(repository.GitCliService);

        await resetService.ResetCurrentBranchToCommitAsync(
            repository.Path,
            repository.InitialCommitHash,
            GitResetMode.Soft,
            CancellationToken.None);

        Assert.Equal(repository.InitialCommitHash, await repository.HeadHashAsync());
        Assert.Equal("after", repository.ReadFile());
        Assert.Equal("M  file.txt", await repository.StatusAsync());
    }

    [Fact]
    public async Task ResetCurrentBranchToCommitAsync_MixedResetKeepsChangeUnstaged()
    {
        using var repository = TemporaryGitRepository.Create();
        var resetService = new GitResetCommandService(repository.GitCliService);

        await resetService.ResetCurrentBranchToCommitAsync(
            repository.Path,
            repository.InitialCommitHash,
            GitResetMode.Mixed,
            CancellationToken.None);

        Assert.Equal(repository.InitialCommitHash, await repository.HeadHashAsync());
        Assert.Equal("after", repository.ReadFile());
        Assert.Equal(" M file.txt", await repository.StatusAsync());
    }

    [Fact]
    public async Task ResetCurrentBranchToCommitAsync_HardResetDiscardsChange()
    {
        using var repository = TemporaryGitRepository.Create();
        var resetService = new GitResetCommandService(repository.GitCliService);

        await resetService.ResetCurrentBranchToCommitAsync(
            repository.Path,
            repository.InitialCommitHash,
            GitResetMode.Hard,
            CancellationToken.None);

        Assert.Equal(repository.InitialCommitHash, await repository.HeadHashAsync());
        Assert.Equal("before", repository.ReadFile());
        Assert.Empty(await repository.StatusAsync());
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(DirectoryInfo directory, GitCliService gitCliService, string initialCommitHash)
        {
            _directory = directory;
            GitCliService = gitCliService;
            InitialCommitHash = initialCommitHash;
            Path = directory.FullName;
        }

        public GitCliService GitCliService { get; }

        public string InitialCommitHash { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-reset-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "file.txt"), "before");
            RunGit(gitCliService, directory.FullName, ["add", "file.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);

            var initialCommitHash = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();

            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "file.txt"), "after");
            RunGit(gitCliService, directory.FullName, ["commit", "-am", "Change file"]);

            return new TemporaryGitRepository(directory, gitCliService, initialCommitHash);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        public async Task<string> HeadHashAsync()
        {
            var result = await GitCliService.ExecuteBufferedAsync(
                ["rev-parse", "HEAD"],
                Path,
                cancellationToken: CancellationToken.None);
            return result.StandardOutput.Trim();
        }

        public string ReadFile()
        {
            return File.ReadAllText(System.IO.Path.Combine(Path, "file.txt"));
        }

        public async Task<string> StatusAsync()
        {
            var result = await GitCliService.ExecuteBufferedAsync(
                ["status", "--short"],
                Path,
                cancellationToken: CancellationToken.None);
            return result.StandardOutput.TrimEnd();
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
