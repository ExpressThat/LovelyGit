using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeWatcherSuppressionTests
{
    [Fact]
    public async Task SuppressionPublishesOneRefreshAfterLargeMutation()
    {
        using var repository = TemporaryDirectory.Create();
        CreateRepositoryLayout(repository.Path);
        var messaging = new RecordingNativeMessaging();
        var suppression = new WorkingTreeWatcherSuppressionCoordinator();
        using var watcher = new WorkingTreeWatcherService(messaging, null!, suppression);
        var repositoryId = Guid.NewGuid();
        await watcher.SwitchActiveRepositoryAsync(repositoryId, repository.Path);

        using (suppression.Suppress(repositoryId))
        {
            for (var index = 0; index < 1_000; index++)
            {
                File.WriteAllText(
                    Path.Combine(repository.Path, $"file-{index:D4}.txt"),
                    "changed");
            }
            await Task.Delay(TimeSpan.FromMilliseconds(150));
            Assert.Equal(0, messaging.WorkingTreeChangedCount);
        }

        await messaging.WorkingTreeChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, messaging.WorkingTreeChangedCount);
        Assert.Empty(messaging.LastObservedChanges);
    }

    [Fact]
    public async Task NestedSuppressionWaitsForFinalScope()
    {
        using var repository = TemporaryDirectory.Create();
        CreateRepositoryLayout(repository.Path);
        var messaging = new RecordingNativeMessaging();
        var suppression = new WorkingTreeWatcherSuppressionCoordinator();
        using var watcher = new WorkingTreeWatcherService(messaging, null!, suppression);
        var repositoryId = Guid.NewGuid();
        await watcher.SwitchActiveRepositoryAsync(repositoryId, repository.Path);

        using var outer = suppression.Suppress(repositoryId);
        using (suppression.Suppress(repositoryId))
        {
            File.WriteAllText(Path.Combine(repository.Path, "changed.txt"), "changed");
        }
        await Task.Delay(TimeSpan.FromMilliseconds(150));
        Assert.Equal(0, messaging.WorkingTreeChangedCount);

        outer.Dispose();
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
        public IReadOnlyList<object> LastObservedChanges { get; private set; } = [];
        public TaskCompletionSource WorkingTreeChanged { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Send<TBody>(
            NativeMessageType messageType,
            NativeMessageResponse<TBody> response,
            JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo) =>
            Record(messageType, response.Body);

        public void Send<TBody>(
            NativeMessageType messageType,
            TBody body,
            JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo) =>
            Record(messageType, body);

        private void Record<TBody>(NativeMessageType messageType, TBody body)
        {
            if (messageType != NativeMessageType.WorkingTreeChanged) return;
            if (body is WorkingTreeChangedNotification notification)
            {
                LastObservedChanges = notification.ObservedChanges;
            }
            Interlocked.Increment(ref _workingTreeChangedCount);
            WorkingTreeChanged.TrySetResult();
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory;
        private TemporaryDirectory(DirectoryInfo directory) => _directory = directory;
        public string Path => _directory.FullName;
        public static TemporaryDirectory Create() =>
            new(Directory.CreateTempSubdirectory("lovelygit-watch-suppress-"));
        public void Dispose() => _directory.Delete(recursive: true);
    }
}
