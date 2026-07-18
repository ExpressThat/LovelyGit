using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Worktrees;
using ExpressThat.LovelyGit.Services.Git.Worktrees;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.Worktrees;

public sealed class GitWorktreeCommandServiceTests
{
    [Fact]
    public void CreateArguments_UsesMeasuredWideCheckoutParallelism()
    {
        var arguments = GitWorktreeCommandService.CreateArguments("C:/linked", "feature/worktree");

        Assert.Equal(
            ["-c", "checkout.workers=8", "worktree", "add", "--", "C:/linked", "feature/worktree"],
            arguments);
    }

    [Fact]
    public async Task CreateLockUnlockAndRemoveAsync_ManagesLinkedWorktree()
    {
        using var repository = TemporaryGitRepository.Create();
        var linkedPath = repository.Path + "-linked";
        var service = CreateService(repository.GitCliService);
        await CreateBranchAsync(repository, "feature/worktree");

        await service.CreateAsync(
            repository.Path, linkedPath, "feature/worktree", CancellationToken.None);
        await service.LockAsync(
            repository.Path, linkedPath, "Portable drive", CancellationToken.None);
        var locked = Assert.Single(await ReadAsync(repository.Path), item => !item.IsCurrent);

        await service.UnlockAsync(repository.Path, linkedPath, CancellationToken.None);
        var unlocked = Assert.Single(await ReadAsync(repository.Path), item => !item.IsCurrent);
        await service.RemoveAsync(repository.Path, linkedPath, force: false, CancellationToken.None);

        Assert.Equal("feature/worktree", locked.BranchName);
        Assert.True(locked.IsLocked);
        Assert.Equal("Portable drive", locked.LockReason);
        Assert.False(unlocked.IsLocked);
        Assert.False(Directory.Exists(linkedPath));
        Assert.Single(await ReadAsync(repository.Path));
    }

    [Fact]
    public async Task CreateAsync_RejectsNonEmptyDestinationBeforeRunningGit()
    {
        using var repository = TemporaryGitRepository.Create();
        var linkedPath = repository.Path + "-non-empty";
        Directory.CreateDirectory(linkedPath);
        await File.WriteAllTextAsync(Path.Combine(linkedPath, "keep.txt"), "keep");
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService(repository.GitCliService).CreateAsync(
                    repository.Path,
                    linkedPath,
                    "feature/worktree",
                    CancellationToken.None));

            Assert.Contains("must be empty", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("keep", await File.ReadAllTextAsync(Path.Combine(linkedPath, "keep.txt")));
        }
        finally
        {
            TemporaryGitDirectory.Delete(new DirectoryInfo(linkedPath));
        }
    }

    [Fact]
    public async Task ValidateExistingAsync_ProtectsCurrentWorktree()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = CreateService(repository.GitCliService);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidateExistingAsync(
                repository.Path,
                repository.Path,
                allowCurrent: false,
                CancellationToken.None));

        Assert.Contains("current worktree", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static GitWorktreeCommandService CreateService(GitCliService gitCliService) =>
        new(new GitOperationService(gitCliService));

    private static async Task CreateBranchAsync(
        TemporaryGitRepository repository,
        string branchName) =>
        _ = await repository.GitCliService.ExecuteBufferedAsync(
            ["branch", branchName],
            repository.Path,
            cancellationToken: CancellationToken.None);

    private static async Task<IReadOnlyList<GitWorktree>> ReadAsync(string repositoryPath)
    {
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            repositoryPath,
            CancellationToken.None);
        return await GitWorktreeReader.ReadAsync(
            paths.GitDirectory,
            paths.WorkTreeDirectory,
            CancellationToken.None);
    }
}
