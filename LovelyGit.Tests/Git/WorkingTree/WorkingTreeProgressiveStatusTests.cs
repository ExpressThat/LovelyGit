using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeProgressiveStatusTests
{
    [Fact]
    public async Task TrackedOnly_ReturnsTrackedChangesWithoutUntrackedFiles()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-progressive-status-");
        await InitializeRepositoryAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "tracked.txt"), "changed content");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "untracked.txt"), "new");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, trackedOnly: true, CancellationToken.None);

        Assert.False(response.IsComplete);
        Assert.Equal("tracked.txt", Assert.Single(response.Unstaged).Path);
        Assert.Empty(response.Untracked);
    }

    [Fact]
    public async Task CompleteScan_ReturnsTheDeferredUntrackedFiles()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-progressive-status-");
        await InitializeRepositoryAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "untracked.txt"), "new");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        Assert.True(response.IsComplete);
        Assert.Equal("untracked.txt", Assert.Single(response.Untracked).Path);
    }

    private static async Task InitializeRepositoryAsync(string path)
    {
        await GitTestProcess.RunAsync(path, "init", "-b", "main");
        await GitTestProcess.RunAsync(path, "config", "user.email", "tests@lovelygit.local");
        await GitTestProcess.RunAsync(path, "config", "user.name", "LovelyGit Tests");
        await File.WriteAllTextAsync(Path.Combine(path, "tracked.txt"), "initial");
        await GitTestProcess.RunAsync(path, "add", "tracked.txt");
        await GitTestProcess.RunAsync(path, "commit", "-m", "initial");
    }
}
