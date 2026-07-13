using ExpressThat.LovelyGit.Services.Git.CommitGraph;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitFileDiffSourceCacheTests
{
    [Fact]
    public void Set_KeepsOnlyTheMostRecentSource()
    {
        var cache = new CommitFileDiffSourceCache();
        var first = Source("old", "new");
        var second = Source("before", "after");

        cache.Set("first", first);
        cache.Set("second", second);

        Assert.False(cache.TryGet("first", out _));
        Assert.True(cache.TryGet("second", out var cached));
        Assert.Same(second, cached);
    }

    [Fact]
    public void Set_DoesNotRetainOversizedText()
    {
        var cache = new CommitFileDiffSourceCache();

        cache.Set("large", Source(new string('a', 2_000_001), string.Empty));

        Assert.False(cache.TryGet("large", out _));
    }

    [Fact]
    public void Clear_ReleasesTheRetainedSource()
    {
        var cache = new CommitFileDiffSourceCache();
        cache.Set("key", Source("old", "new"));

        cache.Clear();

        Assert.False(cache.TryGet("key", out _));
    }

    private static CommitFileDiffService.CommitFileDiffSource Source(string oldText, string newText) => new()
    {
        OldText = oldText,
        NewText = newText,
    };
}
