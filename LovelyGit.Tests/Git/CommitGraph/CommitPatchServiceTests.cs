using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitPatchServiceTests
{
    [Fact]
    public async Task GetCommitPatchAsync_ReturnsUnifiedPatchForCommit()
    {
        using var repository = TemporaryGitRepository.Create();
        repository.WriteFile("sample.txt", "old line\nsame line\n");
        repository.Commit("Add sample");
        repository.WriteFile("sample.txt", "new line\nsame line\n");
        var commitHash = repository.Commit("Update sample");

        var response = await new CommitPatchService().GetCommitPatchAsync(
            repository.Path,
            GitObjectId.Parse(commitHash),
            CancellationToken.None);

        Assert.Equal(commitHash, response.CommitHash);
        Assert.False(response.IsTruncated);
        Assert.Contains("diff --git a/sample.txt b/sample.txt", response.Patch);
        Assert.Contains("--- a/sample.txt", response.Patch);
        Assert.Contains("+++ b/sample.txt", response.Patch);
        Assert.Contains("-old line", response.Patch);
        Assert.Contains("+new line", response.Patch);
    }

    [Fact]
    public async Task GetCommitPatchAsync_FlagsUnsupportedBinaryChanges()
    {
        using var repository = TemporaryGitRepository.Create();
        repository.WriteBytes("image.bin", [0x00, 0x01, 0x02]);
        var commitHash = repository.Commit("Add binary");

        var response = await new CommitPatchService().GetCommitPatchAsync(
            repository.Path,
            GitObjectId.Parse(commitHash),
            CancellationToken.None);

        Assert.True(response.HasUnsupportedBinaryChanges);
        Assert.Contains("Binary files", response.Patch);
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;
        private readonly GitCliService _gitCliService = new();

        private TemporaryGitRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public string Commit(string message)
        {
            RunGit(["add", "."]);
            RunGit(["commit", "-m", message]);
            return RunGit(["rev-parse", "HEAD"]).StandardOutput.Trim();
        }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-patch-");
            var repository = new TemporaryGitRepository(directory);
            repository.RunGit(["init"]);
            repository.RunGit(["config", "user.name", "LovelyGit Test"]);
            repository.RunGit(["config", "user.email", "test@example.invalid"]);
            return repository;
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        public void WriteFile(string relativePath, string contents)
        {
            var path = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
        }

        public void WriteBytes(string relativePath, byte[] contents)
        {
            var path = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, contents);
        }

        private CliWrap.Buffered.BufferedCommandResult RunGit(IReadOnlyList<string> arguments)
        {
            return _gitCliService
                .ExecuteBufferedAsync(arguments, Path)
                .GetAwaiter()
                .GetResult();
        }
    }
}
