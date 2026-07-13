using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictResolutionResponseCacheTests
{
    [Fact]
    public void Cache_IsolatesRepositoriesOptionsAndPaths()
    {
        var cache = new ConflictResolutionResponseCache();
        var exact = Response("exact");
        cache.Set("repo-a", "file.txt", "fingerprint", false, exact);

        Assert.True(cache.TryGet("repo-a", "file.txt", "fingerprint", false, out var found));
        Assert.Same(exact, found);
        Assert.False(cache.TryGet("repo-b", "file.txt", "fingerprint", false, out _));
        Assert.False(cache.TryGet("repo-a", "other.txt", "fingerprint", false, out _));
        Assert.False(cache.TryGet("repo-a", "file.txt", "fingerprint", true, out _));
        Assert.True(cache.TryGetSibling(
            "repo-a", "file.txt", "fingerprint", true, out var sibling, out _));
        Assert.Same(exact, sibling);
    }

    [Fact]
    public void RemoveStale_ReleasesEveryVariantOfPreviousFingerprint()
    {
        var cache = new ConflictResolutionResponseCache();
        cache.Set("repo", "file.txt", "old", false, Response("old-exact"));
        cache.Set("repo", "file.txt", "old", true, Response("old-ignore"));
        cache.Set("repo", "file.txt", "new", false, Response("new"));

        cache.RemoveStale("repo", "file.txt", "new");

        Assert.Equal(1, cache.Count);
        Assert.False(cache.TryGet("repo", "file.txt", "old", false, out _));
        Assert.True(cache.TryGet("repo", "file.txt", "new", false, out _));
    }

    [Fact]
    public void Cache_EvictsLeastRecentlyUsedResponse()
    {
        var cache = new ConflictResolutionResponseCache(capacity: 2);
        cache.Set("repo", "a.txt", "a", false, Response("a"));
        cache.Set("repo", "b.txt", "b", false, Response("b"));
        Assert.True(cache.TryGet("repo", "a.txt", "a", false, out _));
        cache.Set("repo", "c.txt", "c", false, Response("c"));

        Assert.True(cache.TryGet("repo", "a.txt", "a", false, out _));
        Assert.False(cache.TryGet("repo", "b.txt", "b", false, out _));
        Assert.True(cache.TryGet("repo", "c.txt", "c", false, out _));
    }

    [Fact]
    public void Invalidate_RemovesOnlyMatchingRepositoryAndPath()
    {
        var cache = new ConflictResolutionResponseCache();
        cache.Set("repo-a", "file.txt", "a", false, Response("a"));
        cache.Set("repo-a", "other.txt", "b", false, Response("b"));
        cache.Set("repo-b", "file.txt", "c", false, Response("c"));

        cache.Invalidate("repo-a", "file.txt");

        Assert.Equal(2, cache.Count);
        Assert.False(cache.TryGet("repo-a", "file.txt", "a", false, out _));
        Assert.True(cache.TryGet("repo-a", "other.txt", "b", false, out _));
        Assert.True(cache.TryGet("repo-b", "file.txt", "c", false, out _));
    }

    [Fact]
    public void CurrentCache_UsesUnchangedIndexAndResultMetadata()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-conflict-cache-");
        var index = Path.Combine(directory.Path, "index");
        var result = Path.Combine(directory.Path, "file.txt");
        File.WriteAllText(index, "index");
        File.WriteAllText(result, "result");
        var stamp = ConflictResolutionCacheStamp.Capture(index, result);
        var cache = new ConflictResolutionResponseCache();
        var expected = Response("fingerprint");
        cache.Set(directory.Path, "file.txt", "fingerprint", false, expected, stamp);

        Assert.True(cache.TryGetCurrent(
            directory.Path, "file.txt", false, out var response, out var foundStamp));
        Assert.Same(expected, response);
        Assert.Equal(stamp, foundStamp);
    }

    [Fact]
    public void CurrentSiblingCache_ReusesThePreparedOppositeVariant()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-conflict-cache-");
        var index = Path.Combine(directory.Path, "index");
        var result = Path.Combine(directory.Path, "file.txt");
        File.WriteAllText(index, "index");
        File.WriteAllText(result, "result");
        var stamp = ConflictResolutionCacheStamp.Capture(index, result);
        var cache = new ConflictResolutionResponseCache();
        var expected = Response("fingerprint");
        cache.Set(directory.Path, "file.txt", "fingerprint", false, expected, stamp);

        Assert.True(cache.TryGetCurrentSibling(
            directory.Path, "file.txt", true, out var sibling, out var siblingStamp, out _));
        Assert.Same(expected, sibling);
        Assert.Equal(stamp, siblingStamp);
    }

    [Fact]
    public void SiblingCache_ReturnsRetainedComparisonSources()
    {
        var cache = new ConflictResolutionResponseCache();
        var retained = new ConflictTexts("base", "ours", "theirs", null);
        cache.Set(
            "repo", "file.txt", "fingerprint", false, Response("exact"), retainedTexts: retained);

        Assert.True(cache.TryGetSibling(
            "repo", "file.txt", "fingerprint", true, out _, out var found));
        Assert.Equal(retained, found);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CurrentCache_InvalidatesWhenIndexOrResultChanges(bool changeIndex)
    {
        using var directory = TemporaryDirectory.Create("lovelygit-conflict-cache-");
        var index = Path.Combine(directory.Path, "index");
        var result = Path.Combine(directory.Path, "file.txt");
        File.WriteAllText(index, "index");
        File.WriteAllText(result, "result");
        var cache = new ConflictResolutionResponseCache();
        cache.Set(
            directory.Path,
            "file.txt",
            "fingerprint",
            false,
            Response("exact"),
            ConflictResolutionCacheStamp.Capture(index, result));
        cache.Set(directory.Path, "file.txt", "fingerprint", true, Response("sibling"));

        File.AppendAllText(changeIndex ? index : result, " changed");

        Assert.False(cache.TryGetCurrent(directory.Path, "file.txt", false, out _, out _));
        Assert.Equal(0, cache.Count);
    }

    private static ConflictResolutionResponse Response(string fingerprint) => new()
    {
        Path = "file.txt",
        WorktreeFingerprint = fingerprint,
    };
}
