using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitAlternateObjectStoreTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpenAsync_ReadsObjectsFromSharedClone(bool packSourceObjects)
    {
        using var fixture = SharedCloneFixture.Create(packSourceObjects);

        using var repository = await LovelyGitRepository.OpenAsync(
            fixture.ClonePath,
            CancellationToken.None);
        var commit = await repository.GetCommitAsync(
            GitObjectId.Parse(fixture.HeadCommitHash),
            CancellationToken.None);

        Assert.Equal("alternate object", commit.Subject);
    }

    [Fact]
    public async Task OpenAsync_ResolvesRelativeRecursiveAlternatesWithoutLooping()
    {
        using var fixture = SharedCloneFixture.Create(packSourceObjects: false);
        var cloneObjects = Path.Combine(fixture.ClonePath, ".git", "objects");
        var sourceObjects = Path.Combine(fixture.SourcePath, ".git", "objects");
        var cloneAlternates = Path.Combine(cloneObjects, "info", "alternates");
        var sourceAlternates = Path.Combine(sourceObjects, "info", "alternates");
        Directory.CreateDirectory(Path.GetDirectoryName(sourceAlternates)!);
        File.WriteAllLines(cloneAlternates,
        [
            Path.GetRelativePath(cloneObjects, sourceObjects),
            "missing-object-directory",
        ]);
        File.WriteAllText(
            sourceAlternates,
            Path.GetRelativePath(sourceObjects, cloneObjects));

        using var repository = await LovelyGitRepository.OpenAsync(
            fixture.ClonePath,
            CancellationToken.None);
        var commit = await repository.GetCommitAsync(
            GitObjectId.Parse(fixture.HeadCommitHash),
            CancellationToken.None);

        Assert.Equal("alternate object", commit.Subject);
    }

    private sealed class SharedCloneFixture : IDisposable
    {
        private readonly DirectoryInfo _root;
        private readonly GitCliService _git = new();

        private SharedCloneFixture(DirectoryInfo root)
        {
            _root = root;
            SourcePath = Path.Combine(root.FullName, "source");
            ClonePath = Path.Combine(root.FullName, "clone");
        }

        public string SourcePath { get; }

        public string ClonePath { get; }

        public string HeadCommitHash { get; private set; } = "";

        public static SharedCloneFixture Create(bool packSourceObjects)
        {
            var fixture = new SharedCloneFixture(
                Directory.CreateTempSubdirectory("lovelygit-alternates-"));
            fixture.RunGit(fixture._root.FullName, "init", fixture.SourcePath);
            fixture.RunGit(fixture.SourcePath, "config", "user.name", "LovelyGit Test");
            fixture.RunGit(fixture.SourcePath, "config", "user.email", "test@example.invalid");
            File.WriteAllText(Path.Combine(fixture.SourcePath, "file.txt"), "alternate content\n");
            fixture.RunGit(fixture.SourcePath, "add", "file.txt");
            fixture.RunGit(fixture.SourcePath, "commit", "-m", "alternate object");
            fixture.HeadCommitHash = fixture.RunGit(
                fixture.SourcePath,
                "rev-parse",
                "HEAD").StandardOutput.Trim();
            if (packSourceObjects)
            {
                fixture.RunGit(fixture.SourcePath, "gc", "--prune=now");
            }

            fixture.RunGit(
                fixture._root.FullName,
                "clone",
                "--shared",
                fixture.SourcePath,
                fixture.ClonePath);
            return fixture;
        }

        public void Dispose()
        {
            foreach (var file in _root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _root.Delete(recursive: true);
        }

        private CliWrap.Buffered.BufferedCommandResult RunGit(
            string workingDirectory,
            params string[] arguments)
        {
            return _git
                .ExecuteBufferedAsync(arguments, workingDirectory)
                .GetAwaiter()
                .GetResult();
        }
    }
}
