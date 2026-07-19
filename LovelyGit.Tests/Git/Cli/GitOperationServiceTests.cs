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

    [Theory]
    [InlineData(
        "To C:/remote.git\n ! [rejected] tag -> tag (already exists)\nerror: failed to push some refs",
        "! [rejected] tag -> tag (already exists)")]
    [InlineData("transport header\nfatal: Could not read from remote", "fatal: Could not read from remote")]
    [InlineData("transport header\nerror: failed to fetch", "error: failed to fetch")]
    public void GitOperationException_PrefersActionableDiagnostic(
        string standardError,
        string expectedMessage)
    {
        var operation = FailedOperation(standardError);

        var exception = new GitOperationException(operation);

        Assert.StartsWith(expectedMessage, exception.Message);
        Assert.Contains("Try again.", exception.Message);
    }

    [Fact]
    public void GitOperationException_RetainsFirstLineFallbackForUnstructuredErrors()
    {
        var operation = FailedOperation("transport unavailable\nconnection closed");

        var exception = new GitOperationException(operation);

        Assert.StartsWith("transport unavailable", exception.Message);
    }

    private static GitOperationResult FailedOperation(string standardError)
    {
        var now = DateTimeOffset.UtcNow;
        return new GitOperationResult(
            "Push tag",
            "C:/repository",
            ["push"],
            string.Empty,
            standardError,
            1,
            now,
            now,
            "Try again.");
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
