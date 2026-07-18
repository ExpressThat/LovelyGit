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
    public void Set_OversizedTextReleasesThePreviousSource()
    {
        var cache = new CommitFileDiffSourceCache();
        cache.Set("previous", Source("old", "new"));

        cache.Set("large", Source(new string('a', 2_000_001), string.Empty));

        Assert.False(cache.TryGet("previous", out _));
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

    [Fact]
    public void CompressedBundle_SurvivesAnOversizedSourceForTheSameDiff()
    {
        var cache = new CommitFileDiffSourceCache();
        cache.SetCompressedSourceBundle("large", "compressed");

        cache.Set("large", Source(new string('a', 2_000_001), string.Empty));

        Assert.True(cache.TryGetCompressedSourceBundle("large", out var bundle));
        Assert.Equal("compressed", bundle);
        Assert.False(cache.TryGet("large", out _));
    }

    [Fact]
    public void DifferentSource_ReleasesThePreviousCompressedBundle()
    {
        var cache = new CommitFileDiffSourceCache();
        cache.SetCompressedSourceBundle("first", "compressed");

        cache.Set("second", Source("before", "after"));

        Assert.False(cache.TryGetCompressedSourceBundle("first", out _));
    }

    [Fact]
    public void CompressedBundle_RejectsAnOversizedPayloadAndClearsExplicitly()
    {
        var cache = new CommitFileDiffSourceCache();
        cache.SetCompressedSourceBundle("large", new string('a', 2_000_001));
        Assert.False(cache.TryGetCompressedSourceBundle("large", out _));

        cache.SetCompressedSourceBundle("small", "compressed");
        cache.Clear();
        Assert.False(cache.TryGetCompressedSourceBundle("small", out _));
    }

    private static CommitFileDiffService.CommitFileDiffSource Source(string oldText, string newText) => new()
    {
        OldText = oldText,
        NewText = newText,
    };
}
