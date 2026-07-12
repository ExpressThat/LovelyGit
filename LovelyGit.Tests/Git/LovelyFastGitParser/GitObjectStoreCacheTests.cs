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
        var id = GitObjectId.Parse(hash);
        var gitDirectory = Path.Combine(directory.Path, ".git");
        using var store = new GitObjectStore(gitDirectory, GitObjectFormat.Sha1);

        Assert.False(GitObjectStore.IsSharedObjectCached(id));
        var uncached = await store.ReadObjectWithoutCachingAsync(id, CancellationToken.None);
        Assert.Equal(GitObjectKind.Commit, uncached.Kind);
        Assert.False(GitObjectStore.IsSharedObjectCached(id));

        await store.ReadObjectAsync(id, CancellationToken.None);
        Assert.True(GitObjectStore.IsSharedObjectCached(id));
    }
}
