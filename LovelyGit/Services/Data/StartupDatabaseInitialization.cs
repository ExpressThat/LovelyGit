using ExpressThat.LovelyGit.Services.Data.Repositorys;

namespace ExpressThat.LovelyGit.Services.Data;

internal static class StartupDatabaseInitialization
{
    private const int PathIndexGcThreshold = 512;

    public static Task Start(
        Action? prepareAppDatabase = null,
        Action? ensureCacheReady = null)
    {
        prepareAppDatabase ??= PrepareAppDatabase;
        ensureCacheReady ??= GitRepoCacheDbContext.EnsureCacheReady;

        return Task.Factory.StartNew(
            () =>
            {
                prepareAppDatabase();
                ensureCacheReady();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private static void PrepareAppDatabase()
    {
        AppDbContext.RegisterBsonKeys();
        int migratedRepositoryCount;
        using (var context = new AppDbContext())
        {
            var repositories = new KnownGitRepositorysRepository(
                context,
                new KnownGitRepositoryOrderRepository(context));
            migratedRepositoryCount = repositories.EnsurePathIndexAsync().GetAwaiter().GetResult();
        }

        if (migratedRepositoryCount >= PathIndexGcThreshold)
        {
            GC.Collect(
                GC.MaxGeneration,
                GCCollectionMode.Aggressive,
                blocking: true,
                compacting: true);
        }
    }
}
