using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class GitIgnoreMatcherCacheTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"lovelygit-ignore-cache-{Guid.NewGuid():N}");

    public GitIgnoreMatcherCacheTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task TryGet_ReusesMatcherWhileSourcesAreUnchanged()
    {
        var repository = CreateRepository("first", "*.tmp\n");
        var matcher = await LoadAsync(repository);
        var cache = new GitIgnoreMatcherCache();
        cache.Set(repository.WorkTree, repository.GitDirectory, matcher);

        var found = cache.TryGet(repository.WorkTree, repository.GitDirectory, out var cached);

        Assert.True(found);
        Assert.Same(matcher, cached);
    }

    [Fact]
    public async Task TryGet_RejectsMatcherAfterSourceChanges()
    {
        var repository = CreateRepository("changed", "*.tmp\n");
        var cache = await CreateCacheAsync(repository);
        await File.AppendAllTextAsync(Path.Combine(repository.WorkTree, ".gitignore"), "*.cache\n");

        var found = cache.TryGet(repository.WorkTree, repository.GitDirectory, out var cached);

        Assert.False(found);
        Assert.Null(cached);
    }

    [Fact]
    public async Task TryGet_RejectsMatcherWhenMissingSourceIsCreated()
    {
        var repository = CreateRepository("created", null);
        var cache = await CreateCacheAsync(repository);
        await File.WriteAllTextAsync(Path.Combine(repository.WorkTree, ".gitignore"), "*.tmp\n");

        Assert.False(cache.TryGet(repository.WorkTree, repository.GitDirectory, out _));
    }

    [Fact]
    public async Task TryGet_RejectsMatcherAfterConfiguredGlobalExcludesChange()
    {
        var repository = CreateRepository("global", null);
        var excludesPath = Path.Combine(_root, "global-excludes");
        await File.WriteAllTextAsync(excludesPath, "*.tmp\n");
        await File.WriteAllTextAsync(
            Path.Combine(repository.GitDirectory, "config"),
            $"[core]\nexcludesfile = {excludesPath}\n");
        var cache = await CreateCacheAsync(repository);
        await File.AppendAllTextAsync(excludesPath, "*.cache\n");

        Assert.False(cache.TryGet(repository.WorkTree, repository.GitDirectory, out _));
    }

    [Fact]
    public async Task Set_EvictsLeastRecentlyUsedMatcherAtCapacity()
    {
        var first = CreateRepository("one", "one\n");
        var second = CreateRepository("two", "two\n");
        var third = CreateRepository("three", "three\n");
        var cache = new GitIgnoreMatcherCache(capacity: 2);
        cache.Set(first.WorkTree, first.GitDirectory, await LoadAsync(first));
        cache.Set(second.WorkTree, second.GitDirectory, await LoadAsync(second));
        Assert.True(cache.TryGet(first.WorkTree, first.GitDirectory, out _));
        cache.Set(third.WorkTree, third.GitDirectory, await LoadAsync(third));

        Assert.True(cache.TryGet(first.WorkTree, first.GitDirectory, out _));
        Assert.False(cache.TryGet(second.WorkTree, second.GitDirectory, out _));
        Assert.True(cache.TryGet(third.WorkTree, third.GitDirectory, out _));
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private async Task<GitIgnoreMatcherCache> CreateCacheAsync(RepositoryPaths repository)
    {
        var cache = new GitIgnoreMatcherCache();
        cache.Set(repository.WorkTree, repository.GitDirectory, await LoadAsync(repository));
        return cache;
    }

    private static Task<GitIgnoreMatcher> LoadAsync(RepositoryPaths repository) =>
        GitIgnoreMatcher.LoadAsync(repository.WorkTree, repository.GitDirectory, CancellationToken.None);

    private RepositoryPaths CreateRepository(string name, string? ignoreContents)
    {
        var workTree = Path.Combine(_root, name);
        var gitDirectory = Path.Combine(workTree, ".git");
        Directory.CreateDirectory(Path.Combine(gitDirectory, "info"));
        if (ignoreContents != null)
        {
            File.WriteAllText(Path.Combine(workTree, ".gitignore"), ignoreContents);
        }

        return new RepositoryPaths(workTree, gitDirectory);
    }

    private sealed record RepositoryPaths(string WorkTree, string GitDirectory);
}
