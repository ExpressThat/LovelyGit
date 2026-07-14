using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitFileDiffPersistenceTests
{
    [Fact]
    public async Task InteractiveRead_DoesNotRunSynchronousSaveInline()
    {
        using var fixture = RepositoryFixture.Create();
        var saveEntered = Signal();
        var releaseSave = Signal();
        var saveCompleted = Signal();
        CommitFileDiffResponse? cacheResponse = null;
        var cache = new FakeCache
        {
            Save = (response, token) =>
            {
                Assert.False(token.CanBeCanceled);
                saveEntered.TrySetResult();
                releaseSave.Task.GetAwaiter().GetResult();
                cacheResponse = response;
                saveCompleted.TrySetResult();
                return Task.CompletedTask;
            },
        };
        cache.Get = () => cacheResponse;
        using var service = new CommitFileDiffService(cache, true);

        var read = ReadAsync(service, fixture, Guid.Empty);
        await saveEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        try
        {
            var response = await read.WaitAsync(TimeSpan.FromSeconds(1));
            var repeated = await ReadAsync(service, fixture, Guid.Empty);
            Assert.Same(response, repeated);
        }
        finally
        {
            releaseSave.TrySetResult();
        }

        await saveCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Same(cacheResponse, await ReadAsync(service, fixture, Guid.Empty));
    }

    [Fact]
    public async Task FailedSave_IsRemovedSoLaterReadCanRetry()
    {
        using var fixture = RepositoryFixture.Create();
        var saveCount = 0;
        var cache = new FakeCache
        {
            Save = (_, _) =>
            {
                Interlocked.Increment(ref saveCount);
                return Task.FromException(new IOException("cache unavailable"));
            },
        };
        using var service = new CommitFileDiffService(cache, true);

        var first = await ReadAsync(service, fixture, Guid.Empty);
        var retry = first;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (Volatile.Read(ref saveCount) < 2)
        {
            await Task.Delay(1, timeout.Token);
            retry = await ReadAsync(service, fixture, Guid.Empty);
        }

        Assert.True(first.HasDifferences);
        Assert.True(retry.HasDifferences);
    }

    [Fact]
    public async Task CanceledRead_DoesNotSchedulePersistence()
    {
        using var fixture = RepositoryFixture.Create();
        var saveCount = 0;
        var cache = new FakeCache
        {
            Save = (_, _) =>
            {
                Interlocked.Increment(ref saveCount);
                return Task.CompletedTask;
            },
        };
        using var service = new CommitFileDiffService(cache, true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetCommitFileDiffAsync(
                Guid.Empty,
                fixture.Path,
                fixture.CommitId.ToString(),
                "sample.txt",
                CommitDiffViewMode.SideBySide,
                ignoreWhitespace: false,
                cancellation.Token));
        Assert.Equal(0, Volatile.Read(ref saveCount));
    }

    [Fact]
    public async Task StalledSaves_AreSerializedAndRetainOnlyEightPendingResponses()
    {
        using var fixture = RepositoryFixture.Create();
        var saveCount = 0;
        var completedCount = 0;
        var activeCount = 0;
        var maxActiveCount = 0;
        var releaseSaves = Signal();
        var firstStarted = Signal();
        var allCompleted = Signal();
        var cache = new FakeCache
        {
            Save = async (_, _) =>
            {
                var active = Interlocked.Increment(ref activeCount);
                SetMax(ref maxActiveCount, active);
                Interlocked.Increment(ref saveCount);
                firstStarted.TrySetResult();

                await releaseSaves.Task;
                Interlocked.Decrement(ref activeCount);
                if (Interlocked.Increment(ref completedCount) == 8)
                {
                    allCompleted.TrySetResult();
                }
            },
        };
        using var service = new CommitFileDiffService(cache, true);

        for (var index = 0; index < 9; index++)
        {
            var response = await ReadAsync(service, fixture, Guid.NewGuid());
            Assert.True(response.HasDifferences);
        }

        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(1, Volatile.Read(ref saveCount));
        releaseSaves.TrySetResult();
        await allCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(8, Volatile.Read(ref saveCount));
        Assert.Equal(1, Volatile.Read(ref maxActiveCount));
    }

    private static Task<CommitFileDiffResponse> ReadAsync(
        CommitFileDiffService service,
        RepositoryFixture fixture,
        Guid repositoryId) =>
        service.GetCommitFileDiffAsync(
            repositoryId,
            fixture.Path,
            fixture.CommitId.ToString(),
            "sample.txt",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false,
            CancellationToken.None);

    private static TaskCompletionSource Signal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void SetMax(ref int target, int value)
    {
        var current = Volatile.Read(ref target);
        while (value > current)
        {
            var previous = Interlocked.CompareExchange(ref target, value, current);
            if (previous == current) return;
            current = previous;
        }
    }

}
