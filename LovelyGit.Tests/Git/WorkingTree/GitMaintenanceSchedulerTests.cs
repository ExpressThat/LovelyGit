using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class GitMaintenanceSchedulerTests
{
    [Fact]
    public async Task Schedule_RunsMaintenanceOnTheOwnedBackgroundWorker()
    {
        var completed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scheduler = CreateScheduler((path, _) =>
        {
            completed.TrySetResult(path);
            return Task.CompletedTask;
        });
        await scheduler.StartAsync(CancellationToken.None);

        scheduler.Schedule("C:/disposable/repository");

        Assert.Equal("C:/disposable/repository", await completed.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        await scheduler.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task RunnerFailure_DoesNotPreventTheNextMaintenanceRequest()
    {
        var calls = 0;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scheduler = CreateScheduler((_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                throw new InvalidOperationException("maintenance failed");
            }

            completed.TrySetResult();
            return Task.CompletedTask;
        });
        await scheduler.StartAsync(CancellationToken.None);

        scheduler.Schedule("first");
        await WaitForAsync(() => Volatile.Read(ref calls) == 1);
        scheduler.Schedule("second");

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(2, calls);
        await scheduler.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Stop_CancelsActiveMaintenance()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scheduler = CreateScheduler(async (_, cancellationToken) =>
        {
            entered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled.TrySetResult();
                throw;
            }
        });
        await scheduler.StartAsync(CancellationToken.None);
        scheduler.Schedule("active");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await scheduler.StopAsync(CancellationToken.None);

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static GitMaintenanceScheduler CreateScheduler(
        Func<string, CancellationToken, Task> runner) => new(runner, TimeSpan.Zero);

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
