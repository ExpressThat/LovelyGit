using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Merge;

namespace LovelyGit.Tests.Git.Merge;

public sealed class GitMergeCommandServiceTests
{
    [Fact]
    public async Task MergeBranchIntoCurrentAsync_MergesBranchIntoCurrentBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var mergeService = new GitMergeCommandService(repository.GitCliService);

        await mergeService.MergeBranchIntoCurrentAsync(
            repository.Path,
            "feature/source",
            CancellationToken.None);

        Assert.Equal("feature change", File.ReadAllText(repository.FeatureFilePath));
        var mergeBase = await repository.GitCliService.ExecuteBufferedAsync(
            ["merge-base", "--is-ancestor", repository.FeatureCommitHash, "HEAD"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, mergeBase.ExitCode);
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
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
            FeatureFilePath = System.IO.Path.Combine(Path, "feature.txt");
        }

        public GitCliService GitCliService { get; }

        public string FeatureCommitHash { get; }

        public string FeatureFilePath { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-merge-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "Initial"]);
            var defaultBranchName = RunGit(
                gitCliService,
                directory.FullName,
                ["branch", "--show-current"]).StandardOutput.Trim();
            RunGit(gitCliService, directory.FullName, ["checkout", "-b", "feature/source"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "feature.txt"), "feature change");
            RunGit(gitCliService, directory.FullName, ["add", "feature.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Feature"]);
            var featureCommitHash = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();
            RunGit(gitCliService, directory.FullName, ["checkout", defaultBranchName]);

            return new TemporaryGitRepository(directory, gitCliService, featureCommitHash);
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
