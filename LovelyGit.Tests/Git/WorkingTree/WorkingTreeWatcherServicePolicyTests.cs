using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeWatcherServicePolicyTests
{
    [Fact]
    public void GetWorkTreeWatchRoots_ReturnsEmptyForMissingDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Empty(WorkingTreeWatcherService.GetWorkTreeWatchRoots(path));
    }

    [Fact]
    public void GetWorkTreeWatchRoots_ReturnsSingleRecursiveRootForSmallDirectory()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-small-");
        Directory.CreateDirectory(Path.Combine(directory.Path, "src"));
        Directory.CreateDirectory(Path.Combine(directory.Path, "tests"));

        var root = Assert.Single(WorkingTreeWatcherService.GetWorkTreeWatchRoots(directory.Path));
        Assert.Equal(directory.Path, root.Path);
        Assert.True(root.IncludeSubdirectories);
    }

    [Fact]
    public void GetWorkTreeWatchRoots_SplitsLargeDirectoryByTopLevelFolders()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-large-");
        Directory.CreateDirectory(Path.Combine(directory.Path, ".git"));
        for (var index = 0; index < 2001; index++)
        {
            Directory.CreateDirectory(Path.Combine(directory.Path, $"d{index}"));
        }

        var roots = WorkingTreeWatcherService.GetWorkTreeWatchRoots(directory.Path);

        Assert.Equal(2002, roots.Count);
        Assert.Contains(roots, root =>
            root.Path == directory.Path && !root.IncludeSubdirectories);
        Assert.Contains(roots, root =>
            root.Path == Path.Combine(directory.Path, "d2000") && root.IncludeSubdirectories);
        Assert.DoesNotContain(roots, root =>
            root.Path == Path.Combine(directory.Path, ".git"));
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

    [Fact]
    public void ReleaseLargeBuffer_DoesNotThrowForLargeIndex()
    {
        GitIndexMemory.ReleaseLargeBuffer(32 * 1024 * 1024);
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
