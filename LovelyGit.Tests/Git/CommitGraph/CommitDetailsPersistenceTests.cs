using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitDetailsPersistenceTests
{
    [Fact]
    public async Task InteractiveRead_DoesNotRunSynchronousPersistenceWorkInline()
    {
        using var fixture = RepositoryFixture.Create();
        var saveEntered = NewSignal();
        var releaseSave = NewSignal();
        var saveCompleted = NewSignal();
        var service = CreateService(
            (_, _, _) => Task.FromResult<CommitDetailsResponse?>(null),
            (_, _, _, _) =>
            {
                saveEntered.TrySetResult();
                releaseSave.Task.GetAwaiter().GetResult();
                saveCompleted.TrySetResult();
                return Task.CompletedTask;
            });

        var read = ReadInteractiveAsync(service, fixture);
        await saveEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        try
        {
            var details = await read.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Single(details.ChangedFiles);
        }
        finally
        {
            releaseSave.TrySetResult();
        }

        await saveCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task InteractiveRead_ReturnsBeforePersistenceAndCoalescesRepeatedRead()
    {
        using var fixture = RepositoryFixture.Create();
        CommitDetailsResponse? cached = null;
        var saveCount = 0;
        var saveStarted = NewSignal();
        var releaseSave = NewSignal();
        var cacheReady = NewSignal();
        var service = CreateService(
            (_, _, _) => Task.FromResult(cached),
            async (_, _, response, token) =>
            {
                Assert.False(token.CanBeCanceled);
                Interlocked.Increment(ref saveCount);
                saveStarted.TrySetResult();
                await releaseSave.Task;
                cached = response;
                cacheReady.TrySetResult();
            });

        var details = await ReadInteractiveAsync(service, fixture);
        await saveStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var repeated = await ReadInteractiveAsync(service, fixture);

        Assert.Same(details, repeated);
        Assert.Equal(1, Volatile.Read(ref saveCount));
        releaseSave.TrySetResult();
        await cacheReady.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var persisted = await ReadInteractiveAsync(service, fixture);
        Assert.Same(details, persisted);
    }

    [Fact]
    public async Task FailedBackgroundPersistence_DoesNotBlockRetry()
    {
        using var fixture = RepositoryFixture.Create();
        var saveCount = 0;
        var service = CreateService(
            (_, _, _) => Task.FromResult<CommitDetailsResponse?>(null),
            (_, _, _, _) =>
            {
                Interlocked.Increment(ref saveCount);
                return Task.FromException(new IOException("cache unavailable"));
            });

        var first = await ReadInteractiveAsync(service, fixture);
        var retry = first;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (ReferenceEquals(first, retry))
        {
            await Task.Delay(1, timeout.Token);
            retry = await ReadInteractiveAsync(service, fixture);
        }
        while (Volatile.Read(ref saveCount) < 2)
        {
            await Task.Delay(1, timeout.Token);
        }

        Assert.Single(first.ChangedFiles);
        Assert.Single(retry.ChangedFiles);
        Assert.NotSame(first, retry);
        Assert.Equal(2, Volatile.Read(ref saveCount));
    }

    [Fact]
    public async Task CanceledInteractiveRead_DoesNotSchedulePersistence()
    {
        using var fixture = RepositoryFixture.Create();
        var saveCount = 0;
        var service = CreateService(
            (_, _, _) => Task.FromResult<CommitDetailsResponse?>(null),
            (_, _, _, _) =>
            {
                Interlocked.Increment(ref saveCount);
                return Task.CompletedTask;
            });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetCommitDetailsAsync(
                Guid.NewGuid(),
                fixture.Path,
                fixture.CommitId,
                0,
                cancellation.Token));
        Assert.Equal(0, Volatile.Read(ref saveCount));
    }

    [Fact]
    public async Task StalledPersistence_RetainsOnlyBoundedPendingResponses()
    {
        using var fixture = RepositoryFixture.Create();
        var saveCount = 0;
        var completedCount = 0;
        var releaseSaves = NewSignal();
        var savesCompleted = NewSignal();
        var service = CreateService(
            (_, _, _) => Task.FromResult<CommitDetailsResponse?>(null),
            async (_, _, _, _) =>
            {
                Interlocked.Increment(ref saveCount);
                await releaseSaves.Task;
                if (Interlocked.Increment(ref completedCount) == 8)
                {
                    savesCompleted.TrySetResult();
                }
            });

        for (var index = 0; index < 9; index++)
        {
            var response = await service.GetCommitDetailsAsync(
                Guid.NewGuid(),
                fixture.Path,
                fixture.CommitId,
                0,
                CancellationToken.None);
            Assert.Single(response.ChangedFiles);
        }

        Assert.Equal(8, Volatile.Read(ref saveCount));
        releaseSaves.TrySetResult();
        await savesCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static CommitDetailsService CreateService(
        Func<Guid, string, CancellationToken, Task<CommitDetailsResponse?>> get,
        Func<Guid, string, CommitDetailsResponse, CancellationToken, Task> save) =>
        new(get, save, true);

    private static Task<CommitDetailsResponse> ReadInteractiveAsync(
        CommitDetailsService service,
        RepositoryFixture fixture) =>
        service.GetCommitDetailsAsync(
            Guid.Empty,
            fixture.Path,
            fixture.CommitId,
            0,
            CancellationToken.None);

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class RepositoryFixture : IDisposable
    {
        private static readonly RepositoryTemplate<GitObjectId> Template = new(
            "lovelygit-details-persistence-template-",
            Initialize);
        private readonly DirectoryInfo _directory;

        private RepositoryFixture(DirectoryInfo directory, GitObjectId commitId)
        {
            _directory = directory;
            CommitId = commitId;
        }

        public string Path => _directory.FullName;
        public GitObjectId CommitId { get; }

        public static RepositoryFixture Create()
        {
            var (directory, commitId) = Template.CreateCopy("lovelygit-details-persistence-");
            return new RepositoryFixture(directory, commitId);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(true);
        }

        private static GitObjectId Initialize(DirectoryInfo directory)
        {
            InitializedRepositoryTemplate.CopyInto(directory);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "changed.txt"), "changed\n");
            Run(directory, ["add", "changed.txt"]);
            Run(directory, ["commit", "-m", "Changed file"]);
            var hash = Run(directory, ["rev-parse", "HEAD"]).StandardOutput.Trim();
            return GitObjectId.Parse(hash);
        }

        private static CliWrap.Buffered.BufferedCommandResult Run(
            DirectoryInfo directory,
            IReadOnlyList<string> arguments) =>
            new GitCliService().ExecuteBufferedAsync(arguments, directory.FullName)
                .GetAwaiter().GetResult();
    }
}
