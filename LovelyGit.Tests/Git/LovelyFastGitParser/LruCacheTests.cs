using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class LruCacheTests
{
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
}
