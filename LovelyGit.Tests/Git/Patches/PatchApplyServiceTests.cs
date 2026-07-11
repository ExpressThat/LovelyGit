using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Patches;

namespace LovelyGit.Tests.Git.Patches;

public sealed class PatchApplyServiceTests
{
    [Theory]
    [InlineData(false, " M sample.txt")]
    [InlineData(true, "M  sample.txt")]
    public async Task ApplyAsync_PreflightsAndAppliesToRequestedDestination(
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
    public async Task ApplyAsync_WhenPreflightFails_DoesNotChangeWorkingTree()
    {
        using var repository = TestRepository.Create();
        repository.WriteFile("sample.txt", "different\n");
        repository.Commit("Initial");
		using var patch = TemporaryPatch.Create(PatchText.ReplaceLineEndings("\n") + "\n");
        var service = new PatchApplyService(repository.Git);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(
            repository.Path,
			patch.Path,
            stageChanges: false,
            reverse: false,
            CancellationToken.None));

        Assert.Equal("different\n", File.ReadAllText(Path.Combine(repository.Path, "sample.txt")));
        Assert.Empty(repository.RunGit(["status", "--short"]).StandardOutput);
    }

    [Fact]
    public void BuildArguments_IncludesReverseAndCheckOptions()
    {
        var arguments = PatchApplyService.BuildArguments(
            "C:/patches/change.patch",
            stageChanges: true,
            reverse: true,
            checkOnly: true);

        Assert.Equal(
            ["apply", "--check", "--index", "--reverse", "C:/patches/change.patch"],
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
            var repository = new TestRepository(
                Directory.CreateTempSubdirectory("lovelygit-apply-patch-"));
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
