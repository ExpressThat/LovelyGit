using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Cli;

public sealed class GitOperationServiceTests
{
    [Fact]
    public async Task ExecuteBufferedAsync_RecordsCommandResult()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitOperationService(repository.GitCliService);

        var result = await service.ExecuteBufferedAsync(
            "Read HEAD",
            ["rev-parse", "--short", "HEAD"],
            repository.Path,
            "Refresh the repository and try again.",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Read HEAD", result.OperationName);
        Assert.Equal(repository.Path, result.WorkingDirectory);
        Assert.Equal(["rev-parse", "--short", "HEAD"], result.Arguments);
        Assert.Equal(0, result.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(result.StandardOutput));
        Assert.True(result.EndedAt >= result.StartedAt);
        Assert.True(result.Duration >= TimeSpan.Zero);
        Assert.Equal("Refresh the repository and try again.", result.RecoveryHint);
    }

    [Fact]
    public async Task ExecuteRequiredBufferedAsync_ThrowsWithRecordedOperation()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitOperationService(repository.GitCliService);

        var exception = await Assert.ThrowsAsync<GitOperationException>(() =>
            service.ExecuteRequiredBufferedAsync(
                "Delete missing tag",
                ["tag", "-d", "--", "missing-tag"],
                repository.Path,
                "Refresh tags and confirm the tag still exists locally.",
                CancellationToken.None));

        Assert.False(exception.Operation.IsSuccess);
        Assert.Equal("Delete missing tag", exception.Operation.OperationName);
        Assert.NotEqual(0, exception.Operation.ExitCode);
        Assert.Contains("Refresh tags", exception.Message);
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(DirectoryInfo directory, GitCliService gitCliService)
        {
            _directory = directory;
            GitCliService = gitCliService;
            Path = directory.FullName;
        }

        public GitCliService GitCliService { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-operation-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "Initial"]);

            return new TemporaryGitRepository(directory, gitCliService);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        private static void RunGit(
            GitCliService gitCliService,
            string workingDirectory,
            IReadOnlyList<string> arguments)
        {
            gitCliService
                .ExecuteBufferedAsync(arguments, workingDirectory)
                .GetAwaiter()
                .GetResult();
        }
    }
}
