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
        await AssertInvalidDoesNotMutateAsync(path =>
            new GitStashCommandService(new GitOperationService(new GitCliService()))
                .StashChangesAsync(
                path,
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

    [Fact]
    public async Task BranchFromStashAsync_CreatesBranchAtStashBaseAndDropsStash()
    {
        using var repository = TemporaryGitRepository.Create();
        var stashService = new GitStashCommandService(repository.GitOperationService);
        await CreateTrackedStashAsync(repository, stashService);
        await File.AppendAllTextAsync(repository.TrackedPath, "later", CancellationToken.None);
        await RunGitAsync(repository, ["commit", "-am", "Later"]);

        await stashService.BranchFromStashAsync(
            repository.Path,
            "stash@{0}",
            "recover/stashed-work",
            CancellationToken.None);

        var branch = await RunGitAsync(repository, ["branch", "--show-current"]);
        Assert.Equal("recover/stashed-work", branch.StandardOutput.Trim());
        Assert.Contains("changed", await File.ReadAllTextAsync(repository.TrackedPath));
        Assert.Equal(string.Empty, (await StashListAsync(repository)).Trim());
    }

    [Fact]
    public async Task BranchFromStashAsync_ExistingBranchPreservesStashAndCurrentBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var stashService = new GitStashCommandService(repository.GitOperationService);
        await CreateTrackedStashAsync(repository, stashService);
        var originalBranch = (await RunGitAsync(repository, ["branch", "--show-current"]))
            .StandardOutput.Trim();

        await Assert.ThrowsAsync<GitOperationException>(() =>
            stashService.BranchFromStashAsync(
                repository.Path,
                "stash@{0}",
                originalBranch,
                CancellationToken.None));

        var currentBranch = (await RunGitAsync(repository, ["branch", "--show-current"]))
            .StandardOutput.Trim();
        Assert.Equal(originalBranch, currentBranch);
        Assert.Contains("LovelyGit action stash", await StashListAsync(repository));
    }

    [Theory]
    [InlineData("bad branch", "stash@{0}")]
    [InlineData("recover/work", "stash")]
    public async Task BranchFromStashAsync_InvalidInputDoesNotChangeRepository(
        string branchName,
        string selector)
    {
        await AssertInvalidDoesNotMutateAsync(path =>
            new GitStashCommandService(new GitOperationService(new GitCliService()))
                .BranchFromStashAsync(
                path,
                selector,
                branchName,
                CancellationToken.None));
    }

    private static async Task AssertInvalidDoesNotMutateAsync(Func<string, Task> action)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-stash-");
        var sentinel = Path.Combine(directory.FullName, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() => action(directory.FullName));
            Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
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

    private static Task<CliWrap.Buffered.BufferedCommandResult> RunGitAsync(
        TemporaryGitRepository repository,
        IReadOnlyList<string> arguments) =>
        repository.GitCliService.ExecuteBufferedAsync(
            arguments,
            repository.Path,
            cancellationToken: CancellationToken.None);

}
