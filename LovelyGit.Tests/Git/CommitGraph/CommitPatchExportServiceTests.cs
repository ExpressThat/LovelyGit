using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitPatchExportServiceTests
{
    [Fact]
    public async Task ExportAsync_WritesCompleteNativePatchWithoutBom()
    {
        using var repository = TestRepository.Create();
        repository.WriteFile("note.txt", "first\n");
        repository.Commit("Initial");
        repository.WriteFile("note.txt", "second\n");
        var hash = repository.Commit("Update note");
        var outputPath = Path.Combine(repository.Path, "export.patch");
        var picker = new StubSaveFilePicker(outputPath);
        var service = new CommitPatchExportService(new CommitPatchService(), picker);

        var response = await service.ExportAsync(
            repository.Path,
            GitObjectId.Parse(hash),
            CancellationToken.None);

        Assert.True(response.Saved);
        Assert.Equal(outputPath, response.Path);
        var bytes = await File.ReadAllBytesAsync(outputPath);
        Assert.False(bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.Contains("+second", await File.ReadAllTextAsync(outputPath));
        Assert.Equal("Save commit patch", picker.Title);
        Assert.Equal($"{hash[..12]}.patch", picker.SuggestedFileName);
    }

    [Fact]
    public async Task ExportAsync_WhenPickerIsCancelled_DoesNotWriteAFile()
    {
        using var repository = TestRepository.Create();
        repository.WriteFile("note.txt", "content\n");
        var hash = repository.Commit("Initial");
        var service = new CommitPatchExportService(
            new CommitPatchService(),
            new StubSaveFilePicker(null));

        var response = await service.ExportAsync(
            repository.Path,
            GitObjectId.Parse(hash),
            CancellationToken.None);

        Assert.False(response.Saved);
        Assert.Null(response.Path);
    }

    private sealed class StubSaveFilePicker(string? path) : ISaveFilePicker
    {
        public string? SuggestedFileName { get; private set; }
        public string? Title { get; private set; }

        public Task<string?> PickSaveFileAsync(
            string title,
            string suggestedFileName,
            IReadOnlyList<string> extensions,
            CancellationToken cancellationToken)
        {
            Title = title;
            SuggestedFileName = suggestedFileName;
            Assert.Equal(["patch", "diff"], extensions);
            return Task.FromResult(path);
        }
    }

    private sealed class TestRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;
        private readonly GitCliService _git = new();

        private TestRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TestRepository Create()
        {
            var repository = new TestRepository(
                Directory.CreateTempSubdirectory("lovelygit-patch-export-"));
            repository.RunGit(["init"]);
            repository.RunGit(["config", "user.name", "LovelyGit Test"]);
            repository.RunGit(["config", "user.email", "test@example.invalid"]);
            return repository;
        }

        public string Commit(string message)
        {
            RunGit(["add", "."]);
            RunGit(["commit", "-m", message]);
            return RunGit(["rev-parse", "HEAD"]).StandardOutput.Trim();
        }

        public void WriteFile(string relativePath, string contents) =>
            File.WriteAllText(System.IO.Path.Combine(Path, relativePath), contents);

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        private CliWrap.Buffered.BufferedCommandResult RunGit(IReadOnlyList<string> arguments) =>
            _git.ExecuteBufferedAsync(arguments, Path).GetAwaiter().GetResult();
    }
}
