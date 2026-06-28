using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Tags;

namespace LovelyGit.Tests.Git.Tags;

public sealed class GitTagCommandServiceTests
{
    [Fact]
    public async Task CreateTagAsync_CreatesTagAtCommit()
    {
        using var repository = TemporaryGitRepository.Create();
        var tagService = new GitTagCommandService(repository.GitOperationService);

        await tagService.CreateTagAsync(
            repository.Path,
            "v-test-create-tag",
            repository.HeadCommitHash,
            CancellationToken.None);

        var tagRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/tags/v-test-create-tag"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, tagRef.StandardOutput);
    }

    [Fact]
    public async Task DeleteTagAsync_DeletesLocalTag()
    {
        using var repository = TemporaryGitRepository.Create();
        var tagService = new GitTagCommandService(repository.GitOperationService);
        await tagService.CreateTagAsync(
            repository.Path,
            "v-test-delete-tag",
            repository.HeadCommitHash,
            CancellationToken.None);

        await tagService.DeleteTagAsync(
            repository.Path,
            "v-test-delete-tag",
            CancellationToken.None);

        var tagRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/tags/v-test-delete-tag"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(0, tagRef.ExitCode);
    }

    [Fact]
    public async Task PushTagAsync_PushesTagToRemote()
    {
        using var repository = TemporaryGitRepository.Create();
        using var remote = TemporaryBareRepository.Create();
        var tagService = new GitTagCommandService(repository.GitOperationService);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["remote", "add", "lovelygit-test", remote.Path],
            repository.Path,
            cancellationToken: CancellationToken.None);
        await tagService.CreateTagAsync(
            repository.Path,
            "v-test-push-tag",
            repository.HeadCommitHash,
            CancellationToken.None);

        await tagService.PushTagAsync(
            repository.Path,
            "lovelygit-test",
            "v-test-push-tag",
            CancellationToken.None);

        var tagRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/tags/v-test-push-tag"],
            remote.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, tagRef.StandardOutput);
    }

    private sealed class TemporaryBareRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryBareRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryBareRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-tag-remote-");
            new GitCliService()
                .ExecuteBufferedAsync(["init", "--bare"], directory.FullName)
                .GetAwaiter()
                .GetResult();
            return new TemporaryBareRepository(directory);
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
            GitOperationService = new GitOperationService(gitCliService);
            HeadCommitHash = headCommitHash;
            Path = directory.FullName;
        }

        public GitCliService GitCliService { get; }

        public GitOperationService GitOperationService { get; }

        public string HeadCommitHash { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-tag-");
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
