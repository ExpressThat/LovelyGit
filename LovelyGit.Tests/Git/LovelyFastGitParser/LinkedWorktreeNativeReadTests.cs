using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class LinkedWorktreeNativeReadTests
{
    [Fact]
    public async Task NativeReads_UseCommonObjectsAndPerWorktreeHeadAndIndex()
    {
        using var root = TemporaryDirectory.Create("lovelygit-linked-native-");
        var repositoryPath = Path.Combine(root.Path, "repo");
        var linkedPath = Path.Combine(root.Path, "linked");
        Directory.CreateDirectory(repositoryPath);
        await GitTestProcess.RunAsync(repositoryPath, "init");
        await GitTestProcess.RunAsync(repositoryPath, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(repositoryPath, "config", "user.name", "Test User");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "file.txt"), "initial");
        await GitTestProcess.RunAsync(repositoryPath, "add", ".");
        await GitTestProcess.RunAsync(repositoryPath, "commit", "-m", "initial commit");
        var mainBranch = (await GitTestProcess.RunAsync(repositoryPath, "branch", "--show-current")).Trim();
        await GitTestProcess.RunAsync(repositoryPath, "branch", "feature/linked");
        await GitTestProcess.RunAsync(repositoryPath, "worktree", "add", linkedPath, "feature/linked");

        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            linkedPath,
            CancellationToken.None);
        using var repository = await LovelyGitRepository.OpenAsync(linkedPath, CancellationToken.None);
        await File.WriteAllTextAsync(Path.Combine(linkedPath, "file.txt"), "changed content");
        var changes = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(linkedPath, CancellationToken.None);
        var head = await new HeadCommitMessageService().GetAsync(linkedPath, CancellationToken.None);

        Assert.Equal(Path.Combine(repositoryPath, ".git"), paths.GitDirectory);
        Assert.NotEqual(paths.GitDirectory, paths.WorktreeGitDirectory);
        Assert.Equal(linkedPath, paths.WorkTreeDirectory);
        Assert.Equal("feature/linked", repository.CurrentBranchName);
        Assert.Contains(repository.GetBranches(), branch => branch.Name == mainBranch);
        Assert.Contains(repository.GetBranches(), branch => branch.Name == "feature/linked");
        Assert.Contains(changes.Unstaged, change => change.Path == "file.txt");
        Assert.DoesNotContain(changes.Untracked, change => change.Path == ".git");
        Assert.Equal("initial commit", head.Title);
    }
}
