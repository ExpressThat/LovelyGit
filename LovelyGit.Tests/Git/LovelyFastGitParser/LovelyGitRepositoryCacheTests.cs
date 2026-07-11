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

    [Fact]
    public async Task GraphCommitCache_IsSharedWithLaterRepositoryInstance()
    {
        using var temporary = TemporaryGitRepository.Create();
        var commitId = GitObjectId.Parse(temporary.HeadCommitHash);
        using (var graphRepository = await OpenRepositoryAsync(temporary.Path))
        {
            _ = await graphRepository.GetGraphCommitAsync(commitId, CancellationToken.None);
        }

        temporary.RemoveLooseObject(commitId);
        using var detailsRepository = await OpenRepositoryAsync(temporary.Path);
        var commit = await detailsRepository.GetCommitAsync(commitId, CancellationToken.None);

        Assert.Equal("Initial", commit.Subject);
        Assert.NotEmpty(commit.Body);
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

        public void RemoveLooseObject(GitObjectId id)
        {
            var value = id.ToString();
            var path = System.IO.Path.Combine(Path, ".git", "objects", value[..2], value[2..]);
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }

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
