using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeStatusIndexChangesTests
{
    [Fact]
    public void Parser_ReadsCommonChangesAndRenamePaths()
    {
        var response = WorkingTreeStatusListService.ParseStagedNameStatus(
            "M\0modified.txt\0A\0added.txt\0D\0deleted.txt\0R100\0old.txt\0new.txt\0".AsSpan());

        Assert.Collection(
            response,
            file => AssertStatus(file, "modified.txt", "Modified"),
            file => AssertStatus(file, "added.txt", "Added"),
            file => AssertStatus(file, "deleted.txt", "Deleted"),
            file =>
            {
                AssertStatus(file, "new.txt", "Renamed");
                Assert.Equal("old.txt", file.OldPath);
            });
    }

    [Fact]
    public void Parser_ExcludesUnmergedEntries()
    {
        var response = WorkingTreeStatusListService.ParseStagedNameStatus(
            "U\0conflict.txt\0M\0ready.txt\0".AsSpan());

        var file = Assert.Single(response);
        AssertStatus(file, "ready.txt", "Modified");
    }

    [Fact]
    public async Task NativeStatus_CombinesStagedAndUnstagedIndexResults()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-index-status-");
        InitializedRepositoryTemplate.CopyInto(new DirectoryInfo(directory.Path), "master");
        var stagedPath = Path.Combine(directory.Path, "staged.txt");
        var unstagedPath = Path.Combine(directory.Path, "unstaged.txt");
        await File.WriteAllTextAsync(stagedPath, "base");
        await File.WriteAllTextAsync(unstagedPath, "base");
        await GitTestProcess.RunAsync(directory.Path, "add", ".");
        await GitTestProcess.RunAsync(directory.Path, "commit", "-m", "files");
        await File.WriteAllTextAsync(stagedPath, "staged change");
        await File.WriteAllTextAsync(unstagedPath, "unstaged change");
        await GitTestProcess.RunAsync(directory.Path, "add", "staged.txt");

        var response = await new WorkingTreeStatusListService(new GitCliService())
            .GetChangesAsync(directory.Path, CancellationToken.None);

        AssertStatus(Assert.Single(response.Staged), "staged.txt", "Modified");
        var unstaged = Assert.Single(response.Unstaged);
        Assert.Equal("unstaged.txt", unstaged.Path);
        Assert.Equal(WorkingTreeChangeGroup.Unstaged, unstaged.Group);
    }

    private static void AssertStatus(WorkingTreeChangedFile file, string path, string status)
    {
        Assert.Equal(path, file.Path);
        Assert.Equal(status, file.Status);
        Assert.Equal(WorkingTreeChangeGroup.Staged, file.Group);
    }
}
