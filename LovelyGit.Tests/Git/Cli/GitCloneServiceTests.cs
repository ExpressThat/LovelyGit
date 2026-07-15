using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Cli;

public sealed class GitCloneServiceTests
{
    [Fact]
    public async Task CloneAsync_ClonesLocalRemoteWithMonotonicProgress()
    {
        using var source = CloneSourceRepository.Create();
        using var destination = TemporaryDirectory.Create("lovelygit-clone-destination-");
        var progress = new List<GitCloneProgress>();
        var service = new GitCloneService(new GitCliService());

        var path = await service.CloneAsync(
            Guid.NewGuid(),
            source.Path,
            destination.Path,
            "copy",
            shallow: false,
            recurseSubmodules: false,
            progress.Add,
            CancellationToken.None);

        Assert.True(Directory.Exists(System.IO.Path.Combine(path, ".git")));
        Assert.Equal("payload", await File.ReadAllTextAsync(System.IO.Path.Combine(path, "sample.txt")));
        Assert.Equal("Preparing", progress[0].Stage);
        Assert.Equal("Complete", progress[^1].Stage);
        Assert.Equal(100, progress[^1].Percent);
        Assert.True(progress.Zip(progress.Skip(1), (left, right) =>
            right.Percent >= left.Percent).All(value => value));
    }

    [Theory]
    [InlineData("", "copy", "Repository URL is required")]
    [InlineData("remote", "..", "folder name is not valid")]
    [InlineData("remote", "nested/copy", "folder name is not valid")]
    public async Task CloneAsync_RejectsInvalidInputBeforeCreatingDestination(
        string remote,
        string directoryName,
        string expectedMessage)
    {
        using var parent = TemporaryDirectory.Create("lovelygit-clone-validation-");
        var service = new GitCloneService(new GitCliService());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CloneAsync(
            Guid.NewGuid(),
            remote,
            parent.Path,
            directoryName,
            shallow: false,
            recurseSubmodules: false,
            _ => { },
            CancellationToken.None));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFileSystemEntries(parent.Path));
    }

    [Fact]
    public async Task CloneAsync_RejectsExistingDestinationWithoutChangingIt()
    {
        using var source = CloneSourceRepository.Create();
        using var parent = TemporaryDirectory.Create("lovelygit-clone-existing-");
        var destination = Directory.CreateDirectory(System.IO.Path.Combine(parent.Path, "copy"));
        var sentinel = System.IO.Path.Combine(destination.FullName, "keep.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GitCloneService(new GitCliService()).CloneAsync(
                Guid.NewGuid(), source.Path, parent.Path, "copy", false, false,
                _ => { }, CancellationToken.None));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
    }

    [Fact]
    public async Task CloneAsync_FailedRemoteRemovesPartialDestination()
    {
        using var parent = TemporaryDirectory.Create("lovelygit-clone-failure-");
        var destination = System.IO.Path.Combine(parent.Path, "copy");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GitCloneService(new GitCliService()).CloneAsync(
                Guid.NewGuid(),
                System.IO.Path.Combine(parent.Path, "missing-remote"),
                parent.Path,
                "copy",
                false,
                false,
                _ => { },
                CancellationToken.None));

        Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public async Task CloneAsync_CancelRemovesPartialDestinationAndReleasesOperationId()
    {
        using var source = CloneSourceRepository.Create();
        using var parent = TemporaryDirectory.Create("lovelygit-clone-cancel-");
        var operationId = Guid.NewGuid();
        var service = new GitCloneService(new GitCliService());
        var cancelAccepted = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CloneAsync(
            operationId,
            source.Path,
            parent.Path,
            "copy",
            false,
            false,
            progress =>
            {
                if (progress.Stage == "Preparing") cancelAccepted = service.Cancel(operationId);
            },
            CancellationToken.None));

        Assert.True(cancelAccepted);
        Assert.False(Directory.Exists(System.IO.Path.Combine(parent.Path, "copy")));
        Assert.False(service.Cancel(operationId));
    }

    [Fact]
    public async Task DeletePartialDestinationAsync_RetriesFilesReleasedByGit()
    {
        using var parent = TemporaryDirectory.Create("lovelygit-clone-cleanup-");
        var destination = Directory.CreateDirectory(
            System.IO.Path.Combine(parent.Path, "partial")).FullName;
        var packPath = System.IO.Path.Combine(destination, "temporary.pack");
        await File.WriteAllTextAsync(packPath, "partial pack");
        await using var pack = new FileStream(
            packPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var cleanup = GitCloneService.DeletePartialDestinationAsync(
            destination,
            new OperationCanceledException());
        await Task.Delay(125);
        await pack.DisposeAsync();
        await cleanup;

        Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public async Task CloneAsync_RequiresOperationIdBeforeMutation()
    {
        using var parent = TemporaryDirectory.Create("lovelygit-clone-operation-");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            new GitCloneService(new GitCliService()).CloneAsync(
                Guid.Empty, "remote", parent.Path, "copy", false, false,
                _ => { }, CancellationToken.None));

        Assert.Contains("OperationId", exception.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.EnumerateFileSystemEntries(parent.Path));
    }
}

internal sealed class CloneSourceRepository : IDisposable
{
    private readonly TemporaryDirectory _directory;

    private CloneSourceRepository(TemporaryDirectory directory)
    {
        _directory = directory;
        Path = directory.Path;
    }

    public string Path { get; }

    public static CloneSourceRepository Create()
    {
        var directory = TemporaryDirectory.Create("lovelygit-clone-source-");
        var git = new GitCliService();
        Run(git, directory.Path, ["init"]);
        Run(git, directory.Path, ["config", "user.name", "LovelyGit Clone Test"]);
        Run(git, directory.Path, ["config", "user.email", "clone@example.test"]);
        File.WriteAllText(System.IO.Path.Combine(directory.Path, "sample.txt"), "payload");
        Run(git, directory.Path, ["add", "sample.txt"]);
        Run(git, directory.Path, ["commit", "-m", "Initial"]);
        return new CloneSourceRepository(directory);
    }

    public void Dispose() => _directory.Dispose();

    private static void Run(GitCliService git, string path, IReadOnlyList<string> arguments) =>
        git.ExecuteBufferedAsync(arguments, path).GetAwaiter().GetResult();
}

internal sealed class TemporaryDirectory : IDisposable
{
    private readonly DirectoryInfo _directory;

    private TemporaryDirectory(DirectoryInfo directory) => _directory = directory;

    public string Path => _directory.FullName;

    public static TemporaryDirectory Create(string prefix) =>
        new(Directory.CreateTempSubdirectory(prefix));

    public void Dispose()
    {
        foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        _directory.Delete(recursive: true);
    }
}
