using ExpressThat.LovelyGit.Services.Data;

namespace LovelyGit.Tests.Data;

public sealed class StartupDatabaseInitializationTests
{
    [Fact]
    public async Task Start_RunsPreparationOffTheCallingThreadAndCompletesBothSteps()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var callingThread = Environment.CurrentManagedThreadId;
        var workerThread = callingThread;
        var cachePrepared = false;

        var initialization = StartupDatabaseInitialization.Start(
            () =>
            {
                workerThread = Environment.CurrentManagedThreadId;
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5));
            },
            () => cachePrepared = true);

        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            Assert.False(initialization.IsCompleted);
        }
        finally
        {
            release.Set();
        }
        await initialization;

        Assert.NotEqual(callingThread, workerThread);
        Assert.True(cachePrepared);
    }

    [Fact]
    public async Task Start_WhenPreparationFails_PropagatesAndSkipsLaterStep()
    {
        var cachePrepared = false;

        var initialization = StartupDatabaseInitialization.Start(
            () => throw new IOException("database unavailable"),
            () => cachePrepared = true);

        var exception = await Assert.ThrowsAsync<IOException>(() => initialization);

        Assert.Equal("database unavailable", exception.Message);
        Assert.False(cachePrepared);
    }
}
