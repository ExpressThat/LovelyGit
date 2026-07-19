namespace ExpressThat.LovelyGit.Services.Data;

internal static class StartupDatabaseInitialization
{
    public static Task Start(
        Action? registerAppKeys = null,
        Action? ensureCacheReady = null)
    {
        registerAppKeys ??= AppDbContext.RegisterBsonKeys;
        ensureCacheReady ??= GitRepoCacheDbContext.EnsureCacheReady;

        return Task.Factory.StartNew(
            () =>
            {
                registerAppKeys();
                ensureCacheReady();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }
}
