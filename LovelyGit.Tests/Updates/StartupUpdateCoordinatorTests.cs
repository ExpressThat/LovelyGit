using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Updates;
using Microsoft.Extensions.Hosting;

namespace LovelyGit.Tests.Updates;

public sealed class StartupUpdateCoordinatorTests
{
    [Fact]
    public async Task Create_ClientConstructionFailureDisablesUpdates()
    {
        var expected = new InvalidOperationException("manager unavailable");
        Exception? reported = null;
        var coordinator = StartupUpdateCoordinator.Create(
            () => throw expected,
            error => reported = error);

        coordinator.ApplyPendingUpdate([]);
        await coordinator.DownloadAvailableUpdateAsync(CancellationToken.None);

        Assert.Same(expected, reported);
    }

    [Fact]
    public void ApplyPendingUpdate_NotInstalled_DoesNothing()
    {
        var client = new FakeUpdateClient { IsInstalled = false };
        var coordinator = new StartupUpdateCoordinator(client);

        coordinator.ApplyPendingUpdate(["--test"]);

        Assert.Equal(0, client.ApplyCalls);
    }

    [Fact]
    public void ApplyPendingUpdate_ForwardsArguments()
    {
        var client = new FakeUpdateClient();
        var coordinator = new StartupUpdateCoordinator(client);

        coordinator.ApplyPendingUpdate(["--test"]);

        Assert.Equal(1, client.ApplyCalls);
        Assert.NotNull(client.RestartArguments);
        Assert.Equal(["--test"], client.RestartArguments);
    }

    [Fact]
    public void ApplyPendingUpdate_FailureDoesNotEscapeStartup()
    {
        var expected = new InvalidOperationException("broken package");
        var client = new FakeUpdateClient { ApplyFailure = expected };
        Exception? reported = null;
        var coordinator = new StartupUpdateCoordinator(client, error => reported = error);

        coordinator.ApplyPendingUpdate([]);

        Assert.Same(expected, reported);
    }

    [Fact]
    public async Task DownloadAvailableUpdateAsync_NotInstalled_DoesNothing()
    {
        var client = new FakeUpdateClient { IsInstalled = false };
        var coordinator = new StartupUpdateCoordinator(client);

        await coordinator.DownloadAvailableUpdateAsync(CancellationToken.None);

        Assert.Equal(0, client.DownloadCalls);
    }

    [Fact]
    public async Task DownloadAvailableUpdateAsync_FailureIsReportedWithoutEscaping()
    {
        var expected = new InvalidOperationException("feed unavailable");
        var client = new FakeUpdateClient { DownloadFailure = expected };
        Exception? reported = null;
        var coordinator = new StartupUpdateCoordinator(client, error => reported = error);

        await coordinator.DownloadAvailableUpdateAsync(CancellationToken.None);

        Assert.Same(expected, reported);
    }

    [Fact]
    public async Task DownloadAvailableUpdateAsync_CancellationIsQuiet()
    {
        var client = new FakeUpdateClient { WaitForCancellation = true };
        Exception? reported = null;
        var coordinator = new StartupUpdateCoordinator(client, error => reported = error);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await coordinator.DownloadAvailableUpdateAsync(cancellation.Token);

        Assert.Null(reported);
        Assert.Equal(1, client.DownloadCalls);
    }

    [Fact]
    public async Task BackgroundService_StartDoesNotWaitForUpdateNetwork()
    {
        var client = new FakeUpdateClient { WaitForRelease = true };
        var coordinator = new StartupUpdateCoordinator(client);
        using var lifetime = new FakeHostApplicationLifetime();
        using var service = new StartupUpdateBackgroundService(coordinator, lifetime);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var startedAt = Stopwatch.GetTimestamp();

        await service.StartAsync(timeout.Token);
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        Assert.True(elapsed < TimeSpan.FromMilliseconds(50), $"Startup took {elapsed}.");
        Assert.Equal(0, client.DownloadCalls);
        lifetime.NotifyStarted();
        await client.DownloadStarted.Task.WaitAsync(timeout.Token);
        client.ReleaseDownload.TrySetResult();
        await service.StopAsync(timeout.Token);
    }

    private sealed class FakeHostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _started = new();
        private readonly CancellationTokenSource _stopping = new();
        private readonly CancellationTokenSource _stopped = new();

        public CancellationToken ApplicationStarted => _started.Token;
        public CancellationToken ApplicationStopping => _stopping.Token;
        public CancellationToken ApplicationStopped => _stopped.Token;

        public void NotifyStarted() => _started.Cancel();

        public void StopApplication() => _stopping.Cancel();

        public void Dispose()
        {
            _started.Dispose();
            _stopping.Dispose();
            _stopped.Dispose();
        }
    }

    private sealed class FakeUpdateClient : IApplicationUpdateClient
    {
        public bool IsInstalled { get; set; } = true;
        public int ApplyCalls { get; private set; }
        public int DownloadCalls { get; private set; }
        public string[]? RestartArguments { get; private set; }
        public Exception? ApplyFailure { get; init; }
        public Exception? DownloadFailure { get; init; }
        public bool WaitForCancellation { get; init; }
        public bool WaitForRelease { get; init; }
        public TaskCompletionSource DownloadStarted { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseDownload { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public bool TryApplyPendingUpdate(string[] restartArguments)
        {
            ApplyCalls++;
            RestartArguments = restartArguments;
            if (ApplyFailure is not null)
            {
                throw ApplyFailure;
            }

            return true;
        }

        public async Task DownloadAvailableUpdateAsync(CancellationToken cancellationToken)
        {
            DownloadCalls++;
            DownloadStarted.TrySetResult();
            if (DownloadFailure is not null)
            {
                throw DownloadFailure;
            }

            if (WaitForCancellation)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            else if (WaitForRelease)
            {
                await ReleaseDownload.Task.WaitAsync(cancellationToken);
            }
        }
    }
}
