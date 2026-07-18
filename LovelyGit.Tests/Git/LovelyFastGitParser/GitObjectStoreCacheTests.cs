using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitObjectStoreCacheTests
{
    [Fact]
    public async Task ReadObjectWithoutCachingAsync_DoesNotPolluteSharedCache()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-object-cache-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.name", "Test User");
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "unique.txt"),
            Guid.NewGuid().ToString("N"));
        await GitTestProcess.RunAsync(directory.Path, "add", ".");
        await GitTestProcess.RunAsync(directory.Path, "commit", "-m", "unique cache fixture");
        var hash = (await GitTestProcess.RunAsync(directory.Path, "rev-parse", "HEAD")).Trim();
        var blobHash = (await GitTestProcess.RunAsync(
            directory.Path,
            "rev-parse",
            "HEAD:unique.txt")).Trim();
        var id = GitObjectId.Parse(hash);
        var blobId = GitObjectId.Parse(blobHash);
        var gitDirectory = Path.Combine(directory.Path, ".git");
        using var store = new GitObjectStore(gitDirectory, GitObjectFormat.Sha1);

        Assert.False(GitObjectStore.IsSharedObjectCached(id));
        var uncached = await store.ReadObjectWithoutCachingAsync(id, CancellationToken.None);
        Assert.Equal(GitObjectKind.Commit, uncached.Kind);
        Assert.False(GitObjectStore.IsSharedObjectCached(id));

        await store.ReadObjectAsync(id, CancellationToken.None);
        Assert.True(GitObjectStore.IsSharedObjectCached(id));

        using var repository = await LovelyGitRepository.OpenAsync(
            directory.Path,
            CancellationToken.None);
        Assert.False(GitObjectStore.IsSharedObjectCached(blobId));
        var blob = await repository.ReadBlobWithoutCachingAsync(
            blobId,
            CancellationToken.None);
        Assert.NotEmpty(blob);
        Assert.False(GitObjectStore.IsSharedObjectCached(blobId));
    }

    [Fact]
    public async Task ReadObjectWithoutCachingAsync_DoesNotRetainPackedBlobBytes()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-packed-cache-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.name", "Test User");
        var path = Path.Combine(directory.Path, "packed.txt");
        await File.WriteAllTextAsync(path, string.Concat(Enumerable.Repeat(Guid.NewGuid().ToString("N"), 8_192)));
        await GitTestProcess.RunAsync(directory.Path, "add", ".");
        await GitTestProcess.RunAsync(directory.Path, "commit", "-m", "packed cache fixture");
        var blobHash = (await GitTestProcess.RunAsync(directory.Path, "rev-parse", "HEAD:packed.txt")).Trim();
        await GitTestProcess.RunAsync(directory.Path, "gc", "--prune=now");
        using var store = new GitObjectStore(Path.Combine(directory.Path, ".git"), GitObjectFormat.Sha1);
        var id = GitObjectId.Parse(blobHash);

        var uncached = await store.ReadObjectWithoutCachingAsync(id, CancellationToken.None);

        Assert.NotEmpty(uncached.Data);
        Assert.Equal(0, store.PackObjectCacheBytes);
        await store.ReadObjectAsync(id, CancellationToken.None);
        Assert.True(store.PackObjectCacheBytes >= uncached.Data.Length);
    }

    [Fact]
    public async Task ReadObjectWithTransientPackCacheAsync_DoesNotPolluteSharedCache()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-transient-pack-cache-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.name", "Test User");
        await GitTestProcess.RunAsync(
            directory.Path, "commit", "--allow-empty", "-m", "transient cache fixture");
        var hash = (await GitTestProcess.RunAsync(directory.Path, "rev-parse", "HEAD")).Trim();
        await GitTestProcess.RunAsync(directory.Path, "gc", "--prune=now");
        using var store = new GitObjectStore(Path.Combine(directory.Path, ".git"), GitObjectFormat.Sha1);
        var id = GitObjectId.Parse(hash);

        var commit = await store.ReadObjectWithTransientPackCacheAsync(id, CancellationToken.None);

        Assert.Equal(GitObjectKind.Commit, commit.Kind);
        Assert.False(GitObjectStore.IsSharedObjectCached(id));
        Assert.InRange(store.PackObjectCacheBytes, 1, 8 * 1024 * 1024);
    }

    [Fact]
    public async Task RepeatedRepacks_RetireEveryStalePackGeneration()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-pack-retirement-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.name", "Test User");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "packed.txt"), "first");
        await GitTestProcess.RunAsync(directory.Path, "add", ".");
        await GitTestProcess.RunAsync(directory.Path, "commit", "-m", "first pack");
        var firstCommit = GitObjectId.Parse(
            (await GitTestProcess.RunAsync(directory.Path, "rev-parse", "HEAD")).Trim());
        await GitTestProcess.RunAsync(directory.Path, "gc", "--prune=now");
        var gitDirectory = Path.Combine(directory.Path, ".git");
        using var store = new GitObjectStore(gitDirectory, GitObjectFormat.Sha1);

        await store.ReadObjectWithoutCachingAsync(firstCommit, CancellationToken.None);
        Assert.Equal(1, store.OpenPackFileCount);
        Assert.Equal(1, store.OpenPackIndexCount);

        for (var generation = 2; generation <= 6; generation++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(directory.Path, $"packed-{generation}.txt"),
                $"generation {generation}");
            await GitTestProcess.RunAsync(directory.Path, "add", ".");
            await GitTestProcess.RunAsync(
                directory.Path,
                "commit",
                "-m",
                $"pack generation {generation}");
            var currentCommit = GitObjectId.Parse(
                (await GitTestProcess.RunAsync(directory.Path, "rev-parse", "HEAD")).Trim());
            await GitTestProcess.RunAsync(directory.Path, "gc", "--prune=now");
            await store.ReadObjectWithoutCachingAsync(currentCommit, CancellationToken.None);

            Assert.Equal(1, store.OpenPackFileCount);
            Assert.Equal(1, store.OpenPackIndexCount);
        }
    }
}
