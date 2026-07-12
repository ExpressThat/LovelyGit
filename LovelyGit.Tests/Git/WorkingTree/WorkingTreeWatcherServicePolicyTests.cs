using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Text.Json.Serialization.Metadata;

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
    public void GetWorkTreeWatchRoots_UsesRecursiveRootForNestedDirectory()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-large-");
        Directory.CreateDirectory(Path.Combine(directory.Path, ".git"));
        Directory.CreateDirectory(Path.Combine(directory.Path, "src", "feature"));

        var roots = WorkingTreeWatcherService.GetWorkTreeWatchRoots(directory.Path);

        var root = Assert.Single(roots);
        Assert.Equal(directory.Path, root.Path);
        Assert.True(root.IncludeSubdirectories);
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

    [Fact]
    public async Task SwitchActiveRepository_DoesNotPublishSyntheticInvalidation()
    {
        using var repository = TemporaryDirectory.Create("lovelygit-watch-switch-");
        CreateRepositoryLayout(repository.Path);
        var messaging = new RecordingNativeMessaging();
        using var watcher = new WorkingTreeWatcherService(messaging, null!);

        await watcher.SwitchActiveRepositoryAsync(Guid.NewGuid(), repository.Path);

        await Task.Delay(TimeSpan.FromMilliseconds(250));
        Assert.Equal(0, messaging.WorkingTreeChangedCount);
    }

    [Fact]
    public async Task RealFileChange_StillPublishesInvalidationAfterSwitch()
    {
        using var repository = TemporaryDirectory.Create("lovelygit-watch-change-");
        CreateRepositoryLayout(repository.Path);
        var messaging = new RecordingNativeMessaging();
        using var watcher = new WorkingTreeWatcherService(messaging, null!);
        await watcher.SwitchActiveRepositoryAsync(Guid.NewGuid(), repository.Path);

        File.WriteAllText(Path.Combine(repository.Path, "changed.txt"), "changed");

        await messaging.WorkingTreeChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, messaging.WorkingTreeChangedCount);
    }

    private static void CreateRepositoryLayout(string path)
    {
        var gitDirectory = Directory.CreateDirectory(Path.Combine(path, ".git"));
        File.WriteAllText(Path.Combine(gitDirectory.FullName, "HEAD"), "ref: refs/heads/main\n");
    }

    private sealed class RecordingNativeMessaging : INativeMessaging
    {
        private int _workingTreeChangedCount;

        public bool HasWindow => true;
        public int WorkingTreeChangedCount => Volatile.Read(ref _workingTreeChangedCount);
        public TaskCompletionSource WorkingTreeChanged { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Send<TBody>(
            NativeMessageType messageType,
            NativeMessageResponse<TBody> response,
            JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo) =>
            Record(messageType);

        public void Send<TBody>(
            NativeMessageType messageType,
            TBody body,
            JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo) =>
            Record(messageType);

        private void Record(NativeMessageType messageType)
        {
            if (messageType != NativeMessageType.WorkingTreeChanged)
            {
                return;
            }

            Interlocked.Increment(ref _workingTreeChangedCount);
            WorkingTreeChanged.TrySetResult();
        }
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
