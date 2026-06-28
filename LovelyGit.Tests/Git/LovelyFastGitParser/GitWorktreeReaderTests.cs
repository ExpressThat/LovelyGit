using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Worktrees;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitWorktreeReaderTests
{
    [Fact]
    public async Task ReadAsync_ReturnsMainAndLinkedWorktrees()
    {
        using var root = TemporaryDirectory.Create("lovelygit-worktrees-");
        var repositoryPath = Path.Combine(root.Path, "repo");
        var linkedPath = Path.Combine(root.Path, "repo-linked");
        Directory.CreateDirectory(repositoryPath);
        await GitTestProcess.RunAsync(repositoryPath, "init");
        await GitTestProcess.RunAsync(repositoryPath, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(repositoryPath, "config", "user.name", "Test User");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "file.txt"), "main");
        await GitTestProcess.RunAsync(repositoryPath, "add", ".");
        await GitTestProcess.RunAsync(repositoryPath, "commit", "-m", "initial");
        var mainBranch = (await GitTestProcess.RunAsync(repositoryPath, "branch", "--show-current")).Trim();
        await GitTestProcess.RunAsync(repositoryPath, "branch", "feature/worktree");
        await GitTestProcess.RunAsync(
            repositoryPath,
            "worktree",
            "add",
            linkedPath,
            "feature/worktree");
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            repositoryPath,
            CancellationToken.None);

        var worktrees = await GitWorktreeReader.ReadAsync(
            paths.GitDirectory,
            paths.WorkTreeDirectory,
            CancellationToken.None);

        Assert.Collection(
            worktrees,
            worktree =>
            {
                Assert.True(worktree.IsCurrent);
                Assert.Equal(repositoryPath, worktree.Path);
                Assert.Equal(mainBranch, worktree.BranchName);
            },
            worktree =>
            {
                Assert.False(worktree.IsCurrent);
                Assert.Equal(linkedPath, worktree.Path);
                Assert.Equal("feature/worktree", worktree.BranchName);
            });
    }
}
