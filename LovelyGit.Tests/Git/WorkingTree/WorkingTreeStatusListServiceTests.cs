using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeStatusListServiceTests
{
    [Fact]
    public async Task GetChangesAsync_NativePathFindsRootUntrackedFile()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-");
        await CreateInitialCommitAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "launch.tmp"), "hello");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        var file = Assert.Single(response.Untracked);
        AssertStatus(file, "launch.tmp", "Added", WorkingTreeChangeGroup.Untracked);
    }

    [Fact]
    public async Task GetChangesAsync_NativePathFindsUntrackedFileInNewDirectory()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-");
        await CreateInitialCommitAsync(directory.Path);
        var featureDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, "new-feature"));
        await File.WriteAllTextAsync(Path.Combine(featureDirectory.FullName, "launch.tmp"), "hello");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        var file = Assert.Single(response.Untracked);
        AssertStatus(file, "new-feature/launch.tmp", "Added", WorkingTreeChangeGroup.Untracked);
    }

    [Fact]
    public async Task GetChangesAsync_NativePathFindsModifiedTrackedFile()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-");
        await CreateInitialCommitAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "file.txt"), "changed content");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        var file = Assert.Single(response.Unstaged);
        AssertStatus(file, "file.txt", "Modified", WorkingTreeChangeGroup.Unstaged);
    }

    [Fact]
    public async Task GetChangesAsync_ResolvesHeadFromPackedRefs()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-");
        await CreateInitialCommitAsync(directory.Path);
        await GitTestProcess.RunAsync(directory.Path, "pack-refs", "--all", "--prune");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "file.txt"), "changed content");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        var file = Assert.Single(response.Unstaged);
        AssertStatus(file, "file.txt", "Modified", WorkingTreeChangeGroup.Unstaged);
    }

    [Fact]
    public async Task GetChangesAsync_ReturnsTrackedChangesWhenUntrackedScanIsIncomplete()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-");
        await CreateInitialCommitAsync(directory.Path);
        var trackedRoot = Path.Combine(directory.Path, "tracked-root");
        Directory.CreateDirectory(trackedRoot);
        await File.WriteAllTextAsync(Path.Combine(trackedRoot, "tracked.txt"), "tracked");
        await GitTestProcess.RunAsync(directory.Path, "add", ".");
        await GitTestProcess.RunAsync(directory.Path, "commit", "-m", "tracked root");

        for (var index = 0; index < 4001; index++)
        {
            Directory.CreateDirectory(Path.Combine(trackedRoot, $"d{index:D4}"));
        }

        var deepFile = Path.Combine(trackedRoot, "d4000", "deep.tmp");
        await File.WriteAllTextAsync(deepFile, "deep");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "file.txt"), "changed content");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        var file = Assert.Single(response.Unstaged);
        AssertStatus(file, "file.txt", "Modified", WorkingTreeChangeGroup.Unstaged);
    }

    [Fact]
    public void ParsePorcelainStatus_GroupsCommonStatuses()
    {
        var response = WorkingTreeStatusListService.ParsePorcelainStatus(
            "M  staged.txt\0 M unstaged.txt\0?? new.txt\0UU conflicted.txt\0".AsSpan());

        AssertStatus(response.Staged.Single(), "staged.txt", "Modified", WorkingTreeChangeGroup.Staged);
        AssertStatus(response.Unstaged.Single(), "unstaged.txt", "Modified", WorkingTreeChangeGroup.Unstaged);
        AssertStatus(response.Untracked.Single(), "new.txt", "Added", WorkingTreeChangeGroup.Untracked);
        AssertStatus(response.Unmerged.Single(), "conflicted.txt", "Unmerged", WorkingTreeChangeGroup.Unmerged);
    }

    [Fact]
    public void ParsePorcelainStatus_ReadsRenameOldPath()
    {
        var response = WorkingTreeStatusListService.ParsePorcelainStatus("R  new.txt\0old.txt\0".AsSpan());

        var file = response.Staged.Single();
        AssertStatus(file, "new.txt", "Renamed", WorkingTreeChangeGroup.Staged);
        Assert.Equal("old.txt", file.OldPath);
    }

    [Fact]
    public void CountPorcelainRecords_CountsEveryLaunchSummaryRecord()
    {
        var count = WorkingTreeSummaryService.CountPorcelainRecords(
            " M changed.txt\0?? new/a.txt\0?? new/b.txt\0R  renamed.txt\0old.txt\0".AsSpan());

        Assert.Equal(4, count);
    }

    private static void AssertStatus(
        WorkingTreeChangedFile file,
        string path,
        string status,
        WorkingTreeChangeGroup group)
    {
        Assert.Equal(path, file.Path);
        Assert.Equal(status, file.Status);
        Assert.Equal(group, file.Group);
    }

    private static async Task CreateInitialCommitAsync(string path)
    {
        await GitTestProcess.RunAsync(path, "init");
        await GitTestProcess.RunAsync(path, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(path, "config", "user.name", "Test User");
        await File.WriteAllTextAsync(Path.Combine(path, "file.txt"), "hello");
        await GitTestProcess.RunAsync(path, "add", ".");
        await GitTestProcess.RunAsync(path, "commit", "-m", "initial");
    }
}
