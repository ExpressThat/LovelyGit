using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Patches;

namespace LovelyGit.Tests.Git.Patches;

public sealed class PatchApplyServiceTests
{
    [Theory]
    [InlineData(false, " M sample.txt")]
    [InlineData(true, "M  sample.txt")]
    public async Task ApplyAsync_AppliesToRequestedDestination(
        bool stageChanges,
        string expectedStatus)
    {
        using var repository = TestRepository.Create();
        repository.WriteFile("sample.txt", "old\n");
        repository.Commit("Initial");
        using var patch = TemporaryPatch.Create(PatchText.ReplaceLineEndings("\n") + "\n");
        var service = new PatchApplyService(repository.Git);

        await service.ApplyAsync(
            repository.Path,
            patch.Path,
            stageChanges,
            reverse: false,
            CancellationToken.None);

        Assert.Equal("new", File.ReadAllText(Path.Combine(repository.Path, "sample.txt")).Trim());
        Assert.Contains(expectedStatus, repository.RunGit(["status", "--short"]).StandardOutput);
    }

    [Fact]
    public async Task ApplyAsync_WhenPatchFails_DoesNotChangeWorkingTree()
    {
        using var repository = TestRepository.Create();
        repository.WriteFile("sample.txt", "different\n");
        repository.Commit("Initial");
        using var patch = TemporaryPatch.Create(PatchText.ReplaceLineEndings("\n") + "\n");
        var service = new PatchApplyService(repository.Git);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(
            repository.Path,
            patch.Path,
            stageChanges: false,
            reverse: false,
            CancellationToken.None));

        Assert.Contains("sample.txt", exception.Message, StringComparison.Ordinal);
        Assert.Equal("different\n", File.ReadAllText(Path.Combine(repository.Path, "sample.txt")));
        Assert.Empty(repository.RunGit(["status", "--short"]).StandardOutput);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApplyAsync_WhenLaterFileFails_LeavesEarlierFileAndIndexUnchanged(
        bool stageChanges)
    {
        using var repository = TestRepository.Create();
        repository.WriteFile("sample.txt", "old\n");
        repository.WriteFile("other.txt", "actual\n");
        repository.Commit("Initial");
        using var patch = TemporaryPatch.Create(
            PartialFailurePatch.ReplaceLineEndings("\n") + "\n");
        var service = new PatchApplyService(repository.Git);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(
            repository.Path,
            patch.Path,
            stageChanges,
            reverse: false,
            CancellationToken.None));

        Assert.Equal("old\n", File.ReadAllText(Path.Combine(repository.Path, "sample.txt")));
        Assert.Equal("actual\n", File.ReadAllText(Path.Combine(repository.Path, "other.txt")));
        Assert.Empty(repository.RunGit(["status", "--short"]).StandardOutput);
    }

    [Fact]
    public async Task ApplyAsync_ReverseRestoresOriginalContent()
    {
        using var repository = TestRepository.Create();
        repository.WriteFile("sample.txt", "new\n");
        repository.Commit("Initial");
        using var patch = TemporaryPatch.Create(PatchText.ReplaceLineEndings("\n") + "\n");
        var service = new PatchApplyService(repository.Git);

        await service.ApplyAsync(
            repository.Path,
            patch.Path,
            stageChanges: false,
            reverse: true,
            CancellationToken.None);

        Assert.Equal("old", File.ReadAllText(Path.Combine(repository.Path, "sample.txt")).Trim());
    }

    [Fact]
    public async Task ApplyAsync_PreCancelled_LeavesRepositoryUnchanged()
    {
        using var repository = TestRepository.Create();
        repository.WriteFile("sample.txt", "old\n");
        repository.Commit("Initial");
        using var patch = TemporaryPatch.Create(PatchText.ReplaceLineEndings("\n") + "\n");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new PatchApplyService(repository.Git);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ApplyAsync(
            repository.Path,
            patch.Path,
            stageChanges: false,
            reverse: false,
            cancellation.Token));

        Assert.Equal("old\n", File.ReadAllText(Path.Combine(repository.Path, "sample.txt")));
        Assert.Empty(repository.RunGit(["status", "--short"]).StandardOutput);
    }

    [Fact]
    public void BuildArguments_IncludesRequestedOptionsWithoutRedundantPreflight()
    {
        var arguments = PatchApplyService.BuildArguments(
            "C:/patches/change.patch",
            stageChanges: true,
            reverse: true);

        Assert.Equal(
            ["apply", "--index", "--reverse", "C:/patches/change.patch"],
            arguments);
    }

    private const string PatchText = """
        diff --git a/sample.txt b/sample.txt
        index 3367afd..3e75765 100644
        --- a/sample.txt
        +++ b/sample.txt
        @@ -1 +1 @@
        -old
        +new
        """;

    private const string PartialFailurePatch = """
        diff --git a/sample.txt b/sample.txt
        --- a/sample.txt
        +++ b/sample.txt
        @@ -1 +1 @@
        -old
        +new
        diff --git a/other.txt b/other.txt
        --- a/other.txt
        +++ b/other.txt
        @@ -1 +1 @@
        -missing
        +replacement
        """;

	private sealed class TemporaryPatch : IDisposable
	{
		private TemporaryPatch(string path)
		{
			Path = path;
		}

		public string Path { get; }

		public static TemporaryPatch Create(string contents)
		{
			var path = System.IO.Path.Combine(
				System.IO.Path.GetTempPath(),
				$"lovelygit-{Guid.NewGuid():N}.patch");
			File.WriteAllText(path, contents);
			return new TemporaryPatch(path);
		}

		public void Dispose() => File.Delete(Path);
	}

    private sealed class TestRepository : IDisposable
    {
        private static readonly RepositoryTemplate<bool> Template = new(
            "lovelygit-apply-patch-template-",
            InitializeTemplate);
        private readonly DirectoryInfo _directory;

        private TestRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
            Git = new GitCliService();
        }

        public GitCliService Git { get; }
        public string Path { get; }

        public static TestRepository Create()
        {
            var (directory, _) = Template.CreateCopy("lovelygit-apply-patch-");
            return new TestRepository(directory);
        }

        private static bool InitializeTemplate(DirectoryInfo directory)
        {
            var repository = new TestRepository(directory);
            repository.RunGit(["init"]);
            repository.RunGit(["config", "user.name", "LovelyGit Test"]);
            repository.RunGit(["config", "user.email", "test@example.invalid"]);
            return true;
        }

        public string Commit(string message)
        {
            RunGit(["add", "."]);
            RunGit(["commit", "-m", message]);
            return RunGit(["rev-parse", "HEAD"]).StandardOutput.Trim();
        }

        public void WriteFile(string relativePath, string contents) =>
            File.WriteAllText(System.IO.Path.Combine(Path, relativePath), contents);

        public CliWrap.Buffered.BufferedCommandResult RunGit(IReadOnlyList<string> arguments) =>
            Git.ExecuteBufferedAsync(arguments, Path).GetAwaiter().GetResult();

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }
    }
}
