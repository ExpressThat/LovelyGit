using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class LovelyGitRepositoryCacheTests
{
    [Fact]
    public async Task ClearObjectCaches_AllowsCommitToBeReadAgain()
    {
        using var temporary = TemporaryGitRepository.Create();
        using var repository = await OpenRepositoryAsync(temporary.Path);
        var commitId = GitObjectId.Parse(temporary.HeadCommitHash);

        var first = await repository.GetCommitAsync(commitId, CancellationToken.None);
        repository.ClearObjectCaches();
        var second = await repository.GetCommitAsync(commitId, CancellationToken.None);

        Assert.Equal(first.Hash, second.Hash);
        Assert.Equal(first.Subject, second.Subject);
    }

    private static Task<LovelyGitRepository> OpenRepositoryAsync(string path)
    {
        return LovelyGitRepository.OpenAsync(path, CancellationToken.None);
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(DirectoryInfo directory, string headCommitHash)
        {
            _directory = directory;
            HeadCommitHash = headCommitHash;
            Path = directory.FullName;
        }

        public string HeadCommitHash { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-cache-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "Initial"]);

            var headCommitHash = RunGit(
                gitCliService,
                directory.FullName,
                ["rev-parse", "HEAD"]).StandardOutput.Trim();

            return new TemporaryGitRepository(directory, headCommitHash);
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
