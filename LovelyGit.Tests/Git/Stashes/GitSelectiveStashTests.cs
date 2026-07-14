using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Stashes;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace LovelyGit.Tests.Git.Stashes;

public sealed class GitSelectiveStashTests
{
    [Fact]
    public async Task Create_SelectedTrackedPath_LeavesOtherChangesInPlace()
    {
        using var repository = TemporaryGitRepository.Create();
        var otherPath = Path.Combine(repository.Path, "other.txt");
        await File.WriteAllTextAsync(otherPath, "other");
        await RunGitAsync(repository, ["add", "other.txt"]);
        await RunGitAsync(repository, ["commit", "-m", "Add other"]);
        await File.AppendAllTextAsync(repository.TrackedPath, " selected");
        await File.AppendAllTextAsync(otherPath, " remaining");

        await CreateAsync(repository, ["tracked.txt"], includeUntracked: false);

        Assert.Equal("tracked", await File.ReadAllTextAsync(repository.TrackedPath));
        Assert.Equal("other remaining", await File.ReadAllTextAsync(otherPath));
        Assert.Contains("other.txt", await StatusAsync(repository));
        Assert.DoesNotContain("tracked.txt", await StatusAsync(repository));
        Assert.Contains("Selective stash", await StashListAsync(repository));
    }

    [Fact]
    public async Task Create_SelectedUntrackedPath_LeavesOtherUntrackedFileInPlace()
    {
        using var repository = TemporaryGitRepository.Create();
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "selected new.txt"), "selected");
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "remaining.txt"), "remaining");

        await CreateAsync(repository, ["selected new.txt"], includeUntracked: true);

        Assert.False(File.Exists(Path.Combine(repository.Path, "selected new.txt")));
        Assert.True(File.Exists(Path.Combine(repository.Path, "remaining.txt")));
        Assert.Contains("remaining.txt", await StatusAsync(repository));
    }

    [Fact]
    public async Task Create_TreatsPathspecSyntaxAsALiteralFilename()
    {
        using var repository = TemporaryGitRepository.Create();
        const string path = "[ab].txt";
        var fullPath = Path.Combine(repository.Path, path);
        await File.WriteAllTextAsync(fullPath, "original");
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "a.txt"), "other");
        await RunGitAsync(repository, ["--literal-pathspecs", "add", "--", path]);
        await RunGitAsync(repository, ["add", "a.txt"]);
        await RunGitAsync(repository, ["commit", "-m", "Add literal path"]);
        await File.AppendAllTextAsync(fullPath, " changed");
        await File.AppendAllTextAsync(Path.Combine(repository.Path, "a.txt"), " remaining");

        await CreateAsync(repository, [path], includeUntracked: false);

        Assert.Equal("original", await File.ReadAllTextAsync(fullPath));
        Assert.Contains("a.txt", await StatusAsync(repository));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("folder//file.txt")]
    [InlineData("./file.txt")]
    [InlineData(" ")]
    public void Create_InvalidPathFailsBeforeRepositoryDiscovery(string path)
    {
        var service = new GitStashCommandService(new GitOperationService(new GitCliService()));

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = service.ExecuteAsync(
                "not-a-repository",
                StashAction.Create,
                selector: null,
                message: null,
                includeUntracked: false,
                restoreIndex: false,
                selectedOnly: true,
                paths: [path],
                CancellationToken.None);
        });
    }

    [Fact]
    public void Create_SelectedScopeWithoutPathsCannotFallBackToAllChanges()
    {
        var service = new GitStashCommandService(new GitOperationService(new GitCliService()));

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = service.ExecuteAsync(
                "not-a-repository",
                StashAction.Create,
                selector: null,
                message: null,
                includeUntracked: false,
                restoreIndex: false,
                selectedOnly: true,
                paths: [],
                CancellationToken.None);
        });
    }

    [Fact]
    public void Create_PathsWithoutSelectedScopeAreRejected()
    {
        var service = new GitStashCommandService(new GitOperationService(new GitCliService()));

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = service.ExecuteAsync(
                "not-a-repository",
                StashAction.Create,
                selector: null,
                message: null,
                includeUntracked: false,
                restoreIndex: false,
                selectedOnly: false,
                paths: ["tracked.txt"],
                CancellationToken.None);
        });
    }

    [Fact]
    public async Task Create_MissingSelectedPathPreservesChangesAndCreatesNoStash()
    {
        using var repository = TemporaryGitRepository.Create();
        await File.AppendAllTextAsync(repository.TrackedPath, " changed");

        await Assert.ThrowsAsync<GitOperationException>(() =>
            CreateAsync(repository, ["missing.txt"], includeUntracked: false));

        Assert.Contains("tracked.txt", await StatusAsync(repository));
        Assert.Equal(string.Empty, (await StashListAsync(repository)).Trim());
    }

    [Fact]
    public async Task Create_PreCancelledRequestPreservesChangesAndCreatesNoStash()
    {
        using var repository = TemporaryGitRepository.Create();
        await File.AppendAllTextAsync(repository.TrackedPath, " changed");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateAsync(repository, ["tracked.txt"], false, cancellation.Token));

        Assert.Contains("tracked.txt", await StatusAsync(repository));
        Assert.Equal(string.Empty, (await StashListAsync(repository)).Trim());
    }

    private static Task CreateAsync(
        TemporaryGitRepository repository,
        IReadOnlyList<string> paths,
        bool includeUntracked,
        CancellationToken cancellationToken = default) =>
        new GitStashCommandService(repository.GitOperationService).ExecuteAsync(
            repository.Path,
            StashAction.Create,
            selector: null,
            message: "Selective stash",
            includeUntracked,
            restoreIndex: false,
            selectedOnly: true,
            paths,
            cancellationToken);

    private static async Task<string> StatusAsync(TemporaryGitRepository repository) =>
        (await RunGitAsync(repository, ["status", "--short"])).StandardOutput;

    private static async Task<string> StashListAsync(TemporaryGitRepository repository) =>
        (await RunGitAsync(repository, ["stash", "list"])).StandardOutput;

    private static Task<CliWrap.Buffered.BufferedCommandResult> RunGitAsync(
        TemporaryGitRepository repository,
        IReadOnlyList<string> arguments) =>
        repository.GitCliService.ExecuteBufferedAsync(
            arguments,
            repository.Path,
            cancellationToken: CancellationToken.None);
}
