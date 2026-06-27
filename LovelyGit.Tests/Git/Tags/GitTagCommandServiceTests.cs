using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Tags;

namespace LovelyGit.Tests.Git.Tags;

public sealed class GitTagCommandServiceTests
{
    [Fact]
    public async Task CreateTagAsync_CreatesTagAtCommit()
    {
        using var repository = TemporaryGitRepository.Create();
        var tagService = new GitTagCommandService(repository.GitCliService);

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
        var tagService = new GitTagCommandService(repository.GitCliService);
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
