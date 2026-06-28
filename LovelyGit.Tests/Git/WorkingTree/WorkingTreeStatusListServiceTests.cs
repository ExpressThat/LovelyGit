using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeStatusListServiceTests
{
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
}
