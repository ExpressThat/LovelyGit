using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitPatchSeriesServiceTests
{
    [Fact]
    public async Task GetAsync_EmitsApplicableSeriesWithTrailingBlankContext()
    {
        using var repository = SeriesRepository.Create();
        repository.Write("story.txt", "base\n\n");
        repository.Commit("base");
        repository.Write("story.txt", "base\none\n\n");
        var first = repository.Commit("chapter one");
        repository.Write("story.txt", "base\none\ntwo\n\n");
        var second = repository.Commit("chapter two");
        var service = new CommitPatchSeriesService(new CommitPatchService());

        var response = await service.GetAsync(
            repository.Path,
            [GitObjectId.Parse(first), GitObjectId.Parse(second)],
            CancellationToken.None);

        Assert.Equal(2, response.CommitCount);
        Assert.False(response.IsTruncated);
        Assert.False(response.HasUnsupportedBinaryChanges);
        Assert.True(response.Patch.IndexOf("[PATCH 1/2] chapter one", StringComparison.Ordinal) <
                    response.Patch.IndexOf("[PATCH 2/2] chapter two", StringComparison.Ordinal));
        repository.AssertSeriesApplies(response.Patch, ["chapter two", "chapter one"]);
    }

    [Fact]
    public async Task GetAsync_BinaryCommitMarksSeriesUnsafe()
    {
        using var repository = SeriesRepository.Create();
        repository.WriteBytes("image.bin", [0, 1, 2]);
        var hash = repository.Commit("binary");

        var response = await new CommitPatchSeriesService(new CommitPatchService()).GetAsync(
            repository.Path,
            [GitObjectId.Parse(hash)],
            CancellationToken.None);

        Assert.True(response.HasUnsupportedBinaryChanges);
        Assert.Throws<InvalidOperationException>(() =>
            CommitPatchSeriesValidation.ThrowIfUnsafe(response));
    }

    [Fact]
    public async Task GetAsync_CancelledBeforeReadDoesNotProduceOutput()
    {
        using var repository = SeriesRepository.Create();
        repository.Write("file.txt", "content\n");
        var hash = repository.Commit("commit");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new CommitPatchSeriesService(new CommitPatchService()).GetAsync(
                repository.Path,
                [GitObjectId.Parse(hash)],
                cancellation.Token));
    }

    [Fact]
    public async Task ExportAsync_CancelledPickerLeavesDiskUntouched()
    {
        using var repository = SeriesRepository.Create();
        repository.Write("file.txt", "content\n");
        var hash = repository.Commit("commit");
        var output = Path.Combine(repository.Path, "series.patch");
        var picker = new StubPicker(null);
        var service = new CommitPatchSeriesExportService(
            new CommitPatchSeriesService(new CommitPatchService()), picker);

        var response = await service.ExportAsync(
            repository.Path,
            [GitObjectId.Parse(hash)],
            CancellationToken.None);

        Assert.False(response.Saved);
        Assert.False(File.Exists(output));
        Assert.Equal("Save commit patch series", picker.Title);
    }

    private sealed class StubPicker(string? result) : ISaveFilePicker
    {
        public string? Title { get; private set; }

        public Task<string?> PickSaveFileAsync(
            string title,
            string suggestedFileName,
            IReadOnlyList<string> extensions,
            CancellationToken cancellationToken)
        {
            Title = title;
            Assert.Equal("1-commit-series.patch", suggestedFileName);
            Assert.Equal(["patch", "diff"], extensions);
            return Task.FromResult(result);
        }
    }

    private sealed class SeriesRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;
        private readonly GitCliService _git = new();

        private SeriesRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static SeriesRepository Create()
        {
            var repository = new SeriesRepository(
                Directory.CreateTempSubdirectory("lovelygit-patch-series-"));
            repository.Run(["init"]);
            repository.Run(["config", "user.name", "LovelyGit Test"]);
            repository.Run(["config", "user.email", "test@example.invalid"]);
            repository.Run(["config", "core.autocrlf", "false"]);
            return repository;
        }

        public string Commit(string message)
        {
            Run(["add", "."]);
            Run(["commit", "-m", message]);
            return Run(["rev-parse", "HEAD"]).StandardOutput.Trim();
        }

        public void AssertSeriesApplies(string patch, string[] expectedSubjects)
        {
            var patchPath = System.IO.Path.Combine(Path, "series.patch");
            File.WriteAllText(patchPath, patch);
            Run(["checkout", "--detach", "HEAD~2"]);
            Run(["am", patchPath]);
            var subjects = Run(["log", $"-{expectedSubjects.Length}", "--format=%s"])
                .StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(expectedSubjects, subjects);
        }

        public void Write(string name, string value) => File.WriteAllText(System.IO.Path.Combine(Path, name), value);
        public void WriteBytes(string name, byte[] value) => File.WriteAllBytes(System.IO.Path.Combine(Path, name), value);
        private CliWrap.Buffered.BufferedCommandResult Run(IReadOnlyList<string> args) =>
            _git.ExecuteBufferedAsync(args, Path).GetAwaiter().GetResult();

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            _directory.Delete(true);
        }
    }
}
