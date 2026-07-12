using ExpressThat.LovelyGit.Services.Git.CherryPick;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.CherryPick;

public sealed class GitCherryPickCommandServiceTests
{
    [Fact]
    public async Task CherryPickCommitAsync_AppliesCommitToCurrentBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var cherryPickService = new GitCherryPickCommandService(repository.GitCliService);

        await cherryPickService.CherryPickCommitAsync(
            repository.Path,
            repository.FeatureCommitHash,
            CancellationToken.None);

        var headSubject = await repository.GitCliService.ExecuteBufferedAsync(
            ["log", "-1", "--pretty=%s"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.Equal("Feature change", headSubject.StandardOutput.Trim());
        Assert.Equal("feature", File.ReadAllText(Path.Combine(repository.Path, "feature.txt")));
    }

    [Theory]
    [InlineData("not-a-hash", typeof(ArgumentException))]
    [InlineData("0000000000000000000000000000000000000000", typeof(GitOperationException))]
    public async Task CherryPickCommitAsync_InvalidTargetLeavesHeadAndWorkingTreeUnchanged(
        string commitHash,
        Type expectedException)
    {
        if (expectedException == typeof(ArgumentException))
        {
            await AssertInvalidFormatDoesNotCreateRepositoryAsync(commitHash);
            return;
        }

        using var repository = TemporaryGitRepository.Create();
        var before = await repository.ReadHeadAsync();

        var exception = await Record.ExceptionAsync(() =>
            new GitCherryPickCommandService(repository.GitCliService).CherryPickCommitAsync(
                repository.Path,
                commitHash,
                CancellationToken.None));

        Assert.IsType(expectedException, exception);
        Assert.Equal(before, await repository.ReadHeadAsync());
        Assert.False(File.Exists(Path.Combine(repository.Path, "feature.txt")));
        Assert.False(Directory.Exists(Path.Combine(repository.Path, ".git", "sequencer")));
    }

    private static async Task AssertInvalidFormatDoesNotCreateRepositoryAsync(string commitHash)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-cherry-pick-");
        var sentinel = Path.Combine(directory.FullName, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                new GitCherryPickCommandService(new GitCliService()).CherryPickCommitAsync(
                    directory.FullName,
                    commitHash,
                    CancellationToken.None));
            Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private static readonly RepositoryTemplate<string> Template = new(
            "lovelygit-cherry-pick-template-",
            InitializeTemplate);
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(
            DirectoryInfo directory,
            GitCliService gitCliService,
            string featureCommitHash)
        {
            _directory = directory;
            GitCliService = gitCliService;
            FeatureCommitHash = featureCommitHash;
            Path = directory.FullName;
        }

        public GitCliService GitCliService { get; }

        public string FeatureCommitHash { get; }

        public string Path { get; }

        public async Task<string> ReadHeadAsync()
        {
            var result = await GitCliService.ExecuteBufferedAsync(
                ["rev-parse", "HEAD"], Path, cancellationToken: CancellationToken.None);
            return result.StandardOutput.Trim();
        }

        public static TemporaryGitRepository Create()
        {
            var (directory, featureCommitHash) = Template.CreateCopy("lovelygit-cherry-pick-");
            var gitCliService = new GitCliService();
            return new TemporaryGitRepository(directory, gitCliService, featureCommitHash);
        }

        private static string InitializeTemplate(DirectoryInfo directory)
        {
            var gitCliService = new GitCliService();
            InitializedRepositoryTemplate.CopyInto(directory, "master");
            var baseBranchName = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "--abbrev-ref", "HEAD"]).StandardOutput.Trim();
            RunGit(gitCliService, directory.FullName, ["checkout", "-b", "feature"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "feature.txt"), "feature");
            RunGit(gitCliService, directory.FullName, ["add", "feature.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Feature change"]);

            var featureCommitHash = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();
            RunGit(gitCliService, directory.FullName, ["checkout", baseBranchName]);

            return featureCommitHash;
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
