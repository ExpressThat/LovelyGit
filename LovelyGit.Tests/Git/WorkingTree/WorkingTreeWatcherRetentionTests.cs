using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Text.Json.Serialization.Metadata;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeWatcherRetentionTests
{
    [Fact]
    public async Task ObservedPathsExpireAfterTheDuplicateSuppressionWindow()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-retention-");
        var gitDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, ".git"));
        await File.WriteAllTextAsync(
            Path.Combine(gitDirectory.FullName, "HEAD"),
            "ref: refs/heads/main\n");
        var messaging = new RecordingNativeMessaging();
        using var watcher = new WorkingTreeWatcherService(messaging, null!);
        await watcher.SwitchActiveRepositoryAsync(Guid.NewGuid(), directory.Path);

        await File.WriteAllTextAsync(Path.Combine(directory.Path, "changed.txt"), "changed");
        await messaging.Changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.InRange(watcher.RecentObservedChangeCount, 1, int.MaxValue);

        await WaitForAsync(
            () => watcher.RecentObservedChangeCount == 0,
            TimeSpan.FromSeconds(2));
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(20, cancellation.Token);
        }
    }

    private sealed class RecordingNativeMessaging : INativeMessaging
    {
        public bool HasWindow => true;
        public TaskCompletionSource Changed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Send<TBody>(
            NativeMessageType messageType,
            NativeMessageResponse<TBody> response,
            JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo) => Record(messageType);

        public void Send<TBody>(
            NativeMessageType messageType,
            TBody body,
            JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo) => Record(messageType);

        private void Record(NativeMessageType messageType)
        {
            if (messageType == NativeMessageType.WorkingTreeChanged) Changed.TrySetResult();
        }
    }
}
