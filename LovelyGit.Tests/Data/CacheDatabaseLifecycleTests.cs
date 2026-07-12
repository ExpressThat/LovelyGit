using ExpressThat.LovelyGit.Services.Data;

namespace LovelyGit.Tests.Data;

public sealed class CacheDatabaseLifecycleTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"LovelyGit-cache-lifecycle-{Guid.NewGuid():N}");

    [Fact]
    public void IsCurrentRequiresDatabaseAndMatchingVersion()
    {
        Directory.CreateDirectory(_directory);
        var database = Path.Combine(_directory, "cache.blite");
        var marker = database + ".version";

        Assert.False(CacheDatabaseLifecycle.IsCurrent(database, marker, "2"));
        File.WriteAllText(database, "cache");
        Assert.False(CacheDatabaseLifecycle.IsCurrent(database, marker, "2"));
        CacheDatabaseLifecycle.WriteVersion(marker, "1");
        Assert.False(CacheDatabaseLifecycle.IsCurrent(database, marker, "2"));
        CacheDatabaseLifecycle.WriteVersion(marker, "2");
        Assert.True(CacheDatabaseLifecycle.IsCurrent(database, marker, "2"));
    }

    [Fact]
    public void IsCurrentTreatsUnreadableVersionPathAsStale()
    {
        Directory.CreateDirectory(_directory);
        var database = Path.Combine(_directory, "cache.blite");
        var marker = Path.Combine(_directory, "marker");
        File.WriteAllText(database, "cache");
        Directory.CreateDirectory(marker);

        Assert.False(CacheDatabaseLifecycle.IsCurrent(database, marker, "1"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
