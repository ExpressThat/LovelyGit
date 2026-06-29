using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class LruCacheTests
{
    [Fact]
    public void Set_EvictsLeastRecentlyUsedValue_WhenCapacityIsReached()
    {
        var cache = new LruCache<string, int>(2);
        cache.Set("first", 1);
        cache.Set("second", 2);
        Assert.True(cache.TryGet("first", out _));

        cache.Set("third", 3);

        Assert.True(cache.TryGet("first", out _));
        Assert.False(cache.TryGet("second", out _));
        Assert.True(cache.TryGet("third", out _));
    }

    [Fact]
    public void Clear_RemovesCachedValues()
    {
        var cache = new LruCache<string, int>(2);
        cache.Set("first", 1);
        cache.Set("second", 2);

        cache.Clear();

        Assert.False(cache.TryGet("first", out _));
        Assert.False(cache.TryGet("second", out _));
    }

    [Fact]
    public void Set_EvictsLeastRecentlyUsedValues_WhenWeightLimitIsReached()
    {
        var cache = new LruCache<string, string>(4, 6, value => value.Length);
        cache.Set("first", "aaa");
        cache.Set("second", "bbb");
        Assert.True(cache.TryGet("first", out _));

        cache.Set("third", "ccc");

        Assert.True(cache.TryGet("first", out _));
        Assert.False(cache.TryGet("second", out _));
        Assert.True(cache.TryGet("third", out _));
    }

    [Fact]
    public void Set_DoesNotCacheValue_WhenValueExceedsWeightLimit()
    {
        var cache = new LruCache<string, string>(4, 4, value => value.Length);

        cache.Set("large", "12345");

        Assert.False(cache.TryGet("large", out _));
    }
}
