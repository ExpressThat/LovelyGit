using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeTargetedStatusTests
{
    [Fact]
    public async Task GetChangesForPathAsync_ReturnsOnlyRequestedUntrackedFile()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-target-status-");
        InitializedRepositoryTemplate.CopyInto(new DirectoryInfo(directory.Path), "master");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, ".gitignore"), "*.tmp\n");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "other.txt"), "other");

        var response = await CreateService().GetChangesForPathAsync(
            directory.Path,
            ".gitignore",
            CancellationToken.None);

        var file = Assert.Single(response.Untracked);
        Assert.Equal(".gitignore", file.Path);
        Assert.Equal(WorkingTreeChangeGroup.Untracked, file.Group);
        Assert.Empty(response.Staged);
        Assert.Empty(response.Unstaged);
    }

    [Fact]
    public async Task GetChangesForPathAsync_PreservesStagedAndUnstagedStates()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-target-status-");
        InitializedRepositoryTemplate.CopyInto(new DirectoryInfo(directory.Path), "master");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, ".gitignore"), "first.tmp\n");
        await GitTestProcess.RunAsync(directory.Path, "add", ".gitignore");
        await File.AppendAllTextAsync(Path.Combine(directory.Path, ".gitignore"), "second.tmp\n");

        var response = await CreateService().GetChangesForPathAsync(
            directory.Path,
            ".gitignore",
            CancellationToken.None);

        Assert.Equal(".gitignore", Assert.Single(response.Staged).Path);
        Assert.Equal(".gitignore", Assert.Single(response.Unstaged).Path);
        Assert.Empty(response.Untracked);
    }

    private static WorkingTreeStatusListService CreateService() => new(new GitCliService());
}
