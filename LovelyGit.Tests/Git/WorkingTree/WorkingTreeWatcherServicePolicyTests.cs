using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeWatcherServicePolicyTests
{
    [Fact]
    public void ShouldWatchWorkTreeRecursively_ReturnsFalseForMissingDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.False(WorkingTreeWatcherService.ShouldWatchWorkTreeRecursively(path));
    }

    [Fact]
    public void ShouldWatchWorkTreeRecursively_ReturnsTrueForSmallDirectory()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-small-");
        Directory.CreateDirectory(Path.Combine(directory.Path, "src"));
        Directory.CreateDirectory(Path.Combine(directory.Path, "tests"));

        Assert.True(WorkingTreeWatcherService.ShouldWatchWorkTreeRecursively(directory.Path));
    }

    [Fact]
    public void ShouldWatchWorkTreeRecursively_ReturnsTrueForLargeDirectory()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-large-");
        for (var index = 0; index < 2001; index++)
        {
            Directory.CreateDirectory(Path.Combine(directory.Path, $"d{index}"));
        }

        Assert.True(WorkingTreeWatcherService.ShouldWatchWorkTreeRecursively(directory.Path));
    }

    [Fact]
    public void ComputeWorkTreeSnapshot_ChangesForNestedFileEdit()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-snapshot-");
        var nestedDirectory = Path.Combine(directory.Path, "src", "feature");
        Directory.CreateDirectory(nestedDirectory);
        var filePath = Path.Combine(nestedDirectory, "file.txt");
        File.WriteAllText(filePath, "before");
        var before = WorkingTreeWatcherService.ComputeWorkTreeSnapshot(directory.Path, matcher: null);

        File.WriteAllText(filePath, "after-change");
        var after = WorkingTreeWatcherService.ComputeWorkTreeSnapshot(directory.Path, matcher: null);

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void ComputeWorkTreeSnapshot_IgnoresGitDirectoryChanges()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-git-");
        var gitDirectory = Path.Combine(directory.Path, ".git");
        Directory.CreateDirectory(gitDirectory);
        var headPath = Path.Combine(gitDirectory, "HEAD");
        File.WriteAllText(headPath, "ref: refs/heads/main");
        var before = WorkingTreeWatcherService.ComputeWorkTreeSnapshot(directory.Path, matcher: null);

        File.WriteAllText(headPath, "ref: refs/heads/feature");
        var after = WorkingTreeWatcherService.ComputeWorkTreeSnapshot(directory.Path, matcher: null);

        Assert.Equal(before, after);
    }

    [Fact]
    public void MergePendingObservedChange_KeepsAddedWhenCreatedIsFollowedByChanged()
    {
        var pending = new List<WorkingTreeChangedFile>
        {
            new()
            {
                Path = "deep/file.txt",
                Status = "Added",
                Group = WorkingTreeChangeGroup.Untracked,
            },
        };

        WorkingTreeWatcherService.MergePendingObservedChange(
            pending,
            new WorkingTreeChangedFile
            {
                Path = "deep/file.txt",
                Status = "Modified",
                Group = WorkingTreeChangeGroup.Unstaged,
            });

        var change = Assert.Single(pending);
        Assert.Equal("Added", change.Status);
        Assert.Equal(WorkingTreeChangeGroup.Untracked, change.Group);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryDirectory(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryDirectory Create(string prefix)
        {
            return new TemporaryDirectory(Directory.CreateTempSubdirectory(prefix));
        }

        public void Dispose()
        {
            _directory.Delete(recursive: true);
        }
    }
}
