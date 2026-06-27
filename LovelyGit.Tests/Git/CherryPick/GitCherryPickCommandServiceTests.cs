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
        }

        public GitCliService GitCliService { get; }

        public string FeatureCommitHash { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-cherry-pick-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "Initial"]);
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
