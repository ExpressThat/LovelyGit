using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Stashes;

namespace LovelyGit.Tests.Git.Stashes;

public sealed class GitStashCommandServiceTests
{
    [Fact]
    public async Task StashChangesAsync_StashesTrackedChangesByDefault()
    {
        using var repository = TemporaryGitRepository.Create();
        var stashService = new GitStashCommandService(repository.GitOperationService);
        await File.AppendAllTextAsync(
            System.IO.Path.Combine(repository.Path, "tracked.txt"),
            "changed",
            CancellationToken.None);
        await File.WriteAllTextAsync(
            System.IO.Path.Combine(repository.Path, "new.txt"),
            "new",
            CancellationToken.None);

        await stashService.StashChangesAsync(
            repository.Path,
            "LovelyGit test stash",
            includeUntracked: false,
            CancellationToken.None);

        var stashList = await repository.GitCliService.ExecuteBufferedAsync(
            ["stash", "list"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        var status = await repository.GitCliService.ExecuteBufferedAsync(
            ["status", "--short"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.Contains("LovelyGit test stash", stashList.StandardOutput);
        Assert.Contains("?? new.txt", status.StandardOutput);
        Assert.DoesNotContain("tracked.txt", status.StandardOutput);
    }

    [Fact]
    public async Task StashChangesAsync_CanIncludeUntrackedChanges()
    {
        using var repository = TemporaryGitRepository.Create();
        var stashService = new GitStashCommandService(repository.GitOperationService);
        await File.WriteAllTextAsync(
            System.IO.Path.Combine(repository.Path, "new.txt"),
            "new",
            CancellationToken.None);

        await stashService.StashChangesAsync(
            repository.Path,
            "LovelyGit test stash",
            includeUntracked: true,
            CancellationToken.None);

        var status = await repository.GitCliService.ExecuteBufferedAsync(
            ["status", "--short"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.Equal(string.Empty, status.StandardOutput.Trim());
    }

    [Fact]
    public async Task StashChangesAsync_RejectsEmptyMessage()
    {
        using var repository = TemporaryGitRepository.Create();
        var stashService = new GitStashCommandService(repository.GitOperationService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            stashService.StashChangesAsync(
                repository.Path,
                " ",
                includeUntracked: false,
                CancellationToken.None));
    }

    [Fact]
    public async Task ApplyStashAsync_RestoresChangesAndKeepsStash()
    {
        using var repository = TemporaryGitRepository.Create();
        var stashService = new GitStashCommandService(repository.GitOperationService);
        await CreateTrackedStashAsync(repository, stashService);

        await stashService.ApplyStashAsync(repository.Path, "stash", CancellationToken.None);

        Assert.Contains("changed", await File.ReadAllTextAsync(repository.TrackedPath));
        Assert.Contains("LovelyGit action stash", await StashListAsync(repository));
    }

    [Fact]
    public async Task PopStashAsync_RestoresChangesAndRemovesStash()
    {
        using var repository = TemporaryGitRepository.Create();
        var stashService = new GitStashCommandService(repository.GitOperationService);
        await CreateTrackedStashAsync(repository, stashService);

        await stashService.PopStashAsync(repository.Path, "stash", CancellationToken.None);

        Assert.Contains("changed", await File.ReadAllTextAsync(repository.TrackedPath));
        Assert.Equal(string.Empty, (await StashListAsync(repository)).Trim());
    }

    [Fact]
    public async Task DropStashAsync_RemovesStash()
    {
        using var repository = TemporaryGitRepository.Create();
        var stashService = new GitStashCommandService(repository.GitOperationService);
        await CreateTrackedStashAsync(repository, stashService);

        await stashService.DropStashAsync(repository.Path, "stash", CancellationToken.None);

        Assert.Equal(string.Empty, (await StashListAsync(repository)).Trim());
        Assert.DoesNotContain("changed", await File.ReadAllTextAsync(repository.TrackedPath));
    }

    private static async Task CreateTrackedStashAsync(
        TemporaryGitRepository repository,
        GitStashCommandService stashService)
    {
        await File.AppendAllTextAsync(repository.TrackedPath, "changed", CancellationToken.None);
        await stashService.StashChangesAsync(
            repository.Path,
            "LovelyGit action stash",
            includeUntracked: false,
            CancellationToken.None);
    }

    private static async Task<string> StashListAsync(TemporaryGitRepository repository)
    {
        var result = await repository.GitCliService.ExecuteBufferedAsync(
            ["stash", "list"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        return result.StandardOutput;
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(
            DirectoryInfo directory,
            GitCliService gitCliService)
        {
            _directory = directory;
            GitCliService = gitCliService;
            GitOperationService = new GitOperationService(gitCliService);
            Path = directory.FullName;
        }

        public GitCliService GitCliService { get; }

        public GitOperationService GitOperationService { get; }

        public string Path { get; }

        public string TrackedPath => System.IO.Path.Combine(Path, "tracked.txt");

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-stash-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "tracked");
            RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);

            return new TemporaryGitRepository(directory, gitCliService);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        private static CliWrap.Buffered.BufferedCommandResult RunGit(
            GitCliService gitCliService,
            string workingDirectory,
            IReadOnlyList<string> arguments)
        {
            return gitCliService
                .ExecuteBufferedAsync(arguments, workingDirectory)
                .GetAwaiter()
                .GetResult();
        }
    }
}
