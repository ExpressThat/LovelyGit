using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeStatusListServiceTests
{
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-status-template-",
        InitializeTemplate);
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
    public async Task GetChangesAsync_ReturnsTrackedAndRootUntrackedChangesTogether()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-mixed-");
        await CreateInitialCommitAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "file.txt"), "changed content");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "new.txt"), "new content");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        AssertStatus(
            Assert.Single(response.Unstaged),
            "file.txt",
            "Modified",
            WorkingTreeChangeGroup.Unstaged);
        AssertStatus(
            Assert.Single(response.Untracked),
            "new.txt",
            "Added",
            WorkingTreeChangeGroup.Untracked);
    }

    [Fact]
    public async Task GetChangesAsync_DoesNotReportNestedTrackedFilesAsUntracked()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-");
        await CreateInitialCommitAsync(directory.Path);
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, "src", "feature"));
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "first.cs"), "first");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "second.cs"), "second");
        await GitTestProcess.RunAsync(directory.Path, "add", ".");
        await GitTestProcess.RunAsync(directory.Path, "commit", "-m", "nested files");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "file.txt"), "changed content");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        Assert.Empty(response.Untracked);
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

        for (var index = 0; index < 2; index++)
        {
            Directory.CreateDirectory(Path.Combine(trackedRoot, $"d{index:D4}"));
        }

        var deepFile = Path.Combine(trackedRoot, "d0001", "deep.tmp");
        await File.WriteAllTextAsync(deepFile, "deep");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "file.txt"), "changed content");

        var response = await new WorkingTreeStatusListService(
                new GitCliService(),
                maxNativeUntrackedDirectories: 1)
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

    [Fact]
    public async Task GetSummaryAsync_CountsChangesWithoutLoadingFileLists()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-summary-");
        await CreateInitialCommitAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "file.txt"), "changed content");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "new.txt"), "new content");

        var response = await CreateSummaryService()
            .GetSummaryAsync(directory.Path, CancellationToken.None);

        Assert.Equal(2, response.TotalCount);
        Assert.True(response.IsComplete);
    }

    [Fact]
    public async Task GetSummaryAsync_CanReturnFastIncompleteRootUntrackedCount()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-fast-summary-");
        await CreateInitialCommitAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "new-root.txt"), "new content");

        var response = await CreateSummaryService()
            .GetSummaryAsync(
                directory.Path,
                CancellationToken.None,
                allowIncomplete: true);

        Assert.Equal(1, response.TotalCount);
        Assert.False(response.IsComplete);
    }

    [Fact]
    public async Task GetSummaryAsync_FastIncompleteSummaryDoesNotCountTrackedRoot()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-status-fast-summary-");
        await CreateInitialCommitAsync(directory.Path);

        var response = await CreateSummaryService()
            .GetSummaryAsync(
                directory.Path,
                CancellationToken.None,
                allowIncomplete: true);

        Assert.Equal(0, response.TotalCount);
        Assert.False(response.IsComplete);
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

    private static Task CreateInitialCommitAsync(string path)
    {
        Template.CopyInto(new DirectoryInfo(path));
        return Task.CompletedTask;
    }

    private static bool InitializeTemplate(DirectoryInfo directory)
    {
        InitializedRepositoryTemplate.CopyInto(directory, "master");
        File.WriteAllText(Path.Combine(directory.FullName, "file.txt"), "hello");
        GitTestProcess.RunAsync(directory.FullName, "add", ".").GetAwaiter().GetResult();
        GitTestProcess.RunAsync(directory.FullName, "commit", "-m", "initial")
            .GetAwaiter().GetResult();
        return true;
    }

    private static WorkingTreeSummaryService CreateSummaryService() =>
        new(new GitCliService(), new WorkingTreePreliminarySummaryService());
}
