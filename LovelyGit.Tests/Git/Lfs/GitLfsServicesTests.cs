using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Lfs;

namespace LovelyGit.Tests.Git.Lfs;

[Collection(GitShellIntegrationCollection.Name)]
public sealed class GitLfsServicesTests
{
    [Fact]
    public async Task NativeReader_RecognizesOnlyLfsPatternsWithoutStartingGit()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, ".gitattributes"),
            "# generated files\n*.zip filter=lfs diff=lfs merge=lfs -text\n" +
            "docs/** text\n\"large files/**\" filter=lfs -text\n" +
            "*.zip filter=lfs\n");

        var patterns = await NativeGitLfsStateReader.ReadPatternsAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Equal(["*.zip", "large files/**"], patterns);
    }

    [Fact]
    public async Task NativeReader_ReturnsNoPatternsWhenAttributesFileIsMissing()
    {
        using var directory = new TemporaryDirectory();

        var patterns = await NativeGitLfsStateReader.ReadPatternsAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Empty(patterns);
    }

    [Fact]
    public async Task NativeReader_HandlesBomCrLfLongLinesAndNoFinalNewline()
    {
        using var directory = new TemporaryDirectory();
        var longUnrelatedPattern = new string('x', 20_000);
        var contents = $"{longUnrelatedPattern} text\r\n\"large files/**\" filter=lfs -text";
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, ".gitattributes"),
            contents,
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var patterns = await NativeGitLfsStateReader.ReadPatternsAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Equal(["large files/**"], patterns);
    }

    [Fact]
    public async Task NativeReader_WhenCancelled_DoesNotChangeAttributesFile()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, ".gitattributes");
        await File.WriteAllTextAsync(path, "*.zip filter=lfs\n");
        var before = await File.ReadAllBytesAsync(path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            NativeGitLfsStateReader.ReadPatternsAsync(directory.Path, cancellation.Token));

        Assert.Equal(before, await File.ReadAllBytesAsync(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    [InlineData("*.bin\n*.zip")]
    public void TrackAndUntrack_RejectInvalidPatterns(string? pattern)
    {
        Assert.Throws<ArgumentException>(() =>
            GitLfsCommandService.BuildArguments(GitLfsAction.Track, pattern));
        Assert.Throws<ArgumentException>(() =>
            GitLfsCommandService.BuildArguments(GitLfsAction.Untrack, pattern));
    }

    [Fact]
    public void BuildArguments_UsesExplicitLfsSubcommands()
    {
        Assert.Equal(
            ["lfs", "install", "--local"],
            GitLfsCommandService.BuildArguments(GitLfsAction.Install, null));
        Assert.Equal(
            ["lfs", "fetch"],
            GitLfsCommandService.BuildArguments(GitLfsAction.Fetch, null));
        Assert.Equal(
            ["lfs", "pull"],
            GitLfsCommandService.BuildArguments(GitLfsAction.Pull, null));
        Assert.Equal(
            ["lfs", "prune"],
            GitLfsCommandService.BuildArguments(GitLfsAction.Prune, null));
        Assert.Equal(
            ["lfs", "track", "--", "*.psd"],
            GitLfsCommandService.BuildArguments(GitLfsAction.Track, "*.psd"));
    }

    [Fact]
    public async Task TrackAndUntrack_UpdateAttributesAndNativeState()
    {
        using var repository = TemporaryRepository.Create();
        var reader = new NativeGitLfsStateReader(repository.Git);
        var service = new GitLfsCommandService(repository.Git, reader);

        var initial = await reader.ReadAsync(repository.Path, CancellationToken.None);
        Assert.False(initial.IsInitialized);
        await service.ExecuteAsync(
            repository.Path,
            GitLfsAction.Install,
            null,
            CancellationToken.None);

        await service.ExecuteAsync(
            repository.Path,
            GitLfsAction.Track,
            "*.asset",
            CancellationToken.None);
        await service.ExecuteAsync(
            repository.Path,
            GitLfsAction.Track,
            "large files/**",
            CancellationToken.None);
        var tracked = await reader.ReadAsync(repository.Path, CancellationToken.None);
        Assert.True(tracked.IsAvailable);
        Assert.True(tracked.IsInitialized);
        Assert.Equal(["*.asset", "large files/**"], tracked.TrackedPatterns);

        await service.ExecuteAsync(
            repository.Path,
            GitLfsAction.Untrack,
            "*.asset",
            CancellationToken.None);
        await service.ExecuteAsync(
            repository.Path,
            GitLfsAction.Untrack,
            "large files/**",
            CancellationToken.None);
        var untracked = await reader.ReadAsync(repository.Path, CancellationToken.None);
        Assert.Empty(untracked.TrackedPatterns);
    }

    [Fact]
    public async Task InvalidTrackPattern_DoesNotCreateAttributesFile()
    {
        using var directory = new TemporaryDirectory();
        var git = new GitCliService();
        var service = new GitLfsCommandService(git, new NativeGitLfsStateReader(git));

        await Assert.ThrowsAsync<ArgumentException>(() => service.ExecuteAsync(
            directory.Path,
            GitLfsAction.Track,
            "*.asset\n*.secret",
            CancellationToken.None));

        Assert.False(File.Exists(Path.Combine(directory.Path, ".gitattributes")));
    }

    [Fact]
    public async Task PreCancelledTrack_DoesNotCreateAttributesFile()
    {
        using var directory = new TemporaryDirectory();
        var git = new GitCliService();
        var service = new GitLfsCommandService(git, new NativeGitLfsStateReader(git));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExecuteAsync(
            directory.Path,
            GitLfsAction.Track,
            "*.asset",
            cancellation.Token));

        Assert.False(File.Exists(Path.Combine(directory.Path, ".gitattributes")));
    }

    private sealed class TemporaryRepository : IDisposable
    {
        private readonly TemporaryDirectory _directory;

        private TemporaryRepository(TemporaryDirectory directory, GitCliService git)
        {
            _directory = directory;
            Git = git;
        }

        public GitCliService Git { get; }
        public string Path => _directory.Path;

        public static TemporaryRepository Create()
        {
            var directory = new TemporaryDirectory();
            var git = new GitCliService();
            git.ExecuteBufferedAsync(["init"], directory.Path).GetAwaiter().GetResult();
            return new TemporaryRepository(directory, git);
        }

        public void Dispose() => _directory.Dispose();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory =
            Directory.CreateTempSubdirectory("lovelygit-lfs-");

        public string Path => _directory.FullName;

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(true);
        }
    }
}
