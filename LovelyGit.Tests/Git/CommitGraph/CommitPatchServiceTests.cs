using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitPatchServiceTests
{
    [Fact]
    public async Task GetCommitPatchAsync_MissingCommitFailsWithoutWritingFiles()
    {
        using var repository = TemporaryGitRepository.Create();
        repository.WriteFile("sentinel.txt", "unchanged\n");
        var before = File.ReadAllText(Path.Combine(repository.Path, "sentinel.txt"));

        await Assert.ThrowsAsync<FileNotFoundException>(() => new CommitPatchService()
            .GetCommitPatchAsync(
                repository.Path,
                GitObjectId.Parse(new string('0', 40)),
                CancellationToken.None));

        Assert.Equal(before, File.ReadAllText(Path.Combine(repository.Path, "sentinel.txt")));
    }

    [Fact]
    public async Task GetCommitPatchAsync_PreCancelledReadDoesNotProduceOutput()
    {
        using var repository = TemporaryGitRepository.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new CommitPatchService()
            .GetCommitPatchAsync(
                repository.Path,
                GitObjectId.Parse(new string('0', 40)),
                cancellation.Token));
    }

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
        repository.AssertPatchAppliesToParent(response.Patch);
    }

    [Fact]
    public async Task GetCommitPatchAsync_PreservesMissingFinalNewline()
    {
        using var repository = TemporaryGitRepository.Create();
        repository.WriteFile("sample.txt", "old with newline\n");
        repository.Commit("Add sample");
        repository.WriteFile("sample.txt", "new without newline");
        var commitHash = repository.Commit("Update sample");

        var response = await new CommitPatchService().GetCommitPatchAsync(
            repository.Path, GitObjectId.Parse(commitHash), CancellationToken.None);

        Assert.Contains("\\ No newline at end of file", response.Patch, StringComparison.Ordinal);
        repository.AssertPatchAppliesToParent(response.Patch);
    }

    [Fact]
    public async Task GetCommitPatchAsync_PreservesRequiredBlankContextAtPatchEnd()
    {
        using var repository = TemporaryGitRepository.Create();
        repository.WriteFile("sample.txt", "first\nold\nlast\n\n");
        repository.Commit("Add sample");
        repository.WriteFile("sample.txt", "first\nnew\nlast\n\n");
        var commitHash = repository.Commit("Update sample");

        var response = await new CommitPatchService().GetCommitPatchAsync(
            repository.Path, GitObjectId.Parse(commitHash), CancellationToken.None);

        Assert.Contains(" last\n \n", response.Patch.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        repository.AssertPatchAppliesToParent(response.Patch);
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

    [Fact]
    public async Task GetCommitPatchAsync_EmitsApplicableNewFileHeaders()
    {
        using var repository = TemporaryGitRepository.Create();
        repository.WriteFile("base.txt", "base\n");
        repository.Commit("Base");
        repository.WriteFile("added.txt", "added\n");
        var commitHash = repository.Commit("Add file");

        var response = await new CommitPatchService().GetCommitPatchAsync(
            repository.Path, GitObjectId.Parse(commitHash), CancellationToken.None);

        Assert.Contains("new file mode 100644", response.Patch, StringComparison.Ordinal);
        Assert.Contains("--- /dev/null", response.Patch, StringComparison.Ordinal);
        repository.AssertPatchAppliesToParent(response.Patch);
    }

    [Fact]
    public async Task GetCommitPatchAsync_EmitsApplicableDeletedFileHeaders()
    {
        using var repository = TemporaryGitRepository.Create();
        repository.WriteFile("deleted.txt", "deleted\n");
        repository.Commit("Add file");
        repository.DeleteFile("deleted.txt");
        var commitHash = repository.Commit("Delete file");

        var response = await new CommitPatchService().GetCommitPatchAsync(
            repository.Path, GitObjectId.Parse(commitHash), CancellationToken.None);

        Assert.Contains("deleted file mode 100644", response.Patch, StringComparison.Ordinal);
        Assert.Contains("+++ /dev/null", response.Patch, StringComparison.Ordinal);
        repository.AssertPatchAppliesToParent(response.Patch);
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
            repository.RunGit(["config", "core.autocrlf", "false"]);
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

        public void DeleteFile(string relativePath) =>
            File.Delete(System.IO.Path.Combine(Path, relativePath));

        public void AssertPatchAppliesToParent(string patch)
        {
            var patchPath = System.IO.Path.GetTempFileName();
            try
            {
                File.WriteAllText(patchPath, patch);
                RunGit(["checkout", "--detach", "HEAD^"]);
                RunGit(["apply", "--check", patchPath]);
            }
            finally
            {
                File.Delete(patchPath);
            }
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
