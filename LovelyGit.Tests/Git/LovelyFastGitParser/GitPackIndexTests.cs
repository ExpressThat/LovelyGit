using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitPackIndexTests
{
    [Fact]
    public async Task TryFindOffset_ReadsPackedObjectOffsetsThroughPageCache()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-pack-index-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.name", "Test User");
        await GitTestProcess.RunAsync(
            directory.Path, "commit", "--allow-empty", "-m", "page cache fixture");
        var hash = (await GitTestProcess.RunAsync(directory.Path, "rev-parse", "HEAD")).Trim();
        await GitTestProcess.RunAsync(directory.Path, "gc", "--prune=now");
        var indexPath = Assert.Single(Directory.GetFiles(
            Path.Combine(directory.Path, ".git", "objects", "pack"), "*.idx"));
        using var index = await GitPackIndex.OpenAsync(
            indexPath, GitObjectFormat.Sha1, CancellationToken.None);

        var first = index.TryFindOffset(GitObjectId.Parse(hash), CancellationToken.None);
        var second = index.TryFindOffset(GitObjectId.Parse(hash), CancellationToken.None);

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.InRange(GitPackIndex.CachedIndexBytes, 1, 8 * 1024 * 1024);
        Assert.Null(index.TryFindOffset(
            GitObjectId.Parse("0000000000000000000000000000000000000000"),
            CancellationToken.None));
    }

    [Fact]
    public async Task TryFindOffset_HonorsCancellationBeforeReading()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-pack-index-cancel-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(directory.Path, "config", "user.name", "Test User");
        await GitTestProcess.RunAsync(
            directory.Path, "commit", "--allow-empty", "-m", "cancellation fixture");
        await GitTestProcess.RunAsync(directory.Path, "gc", "--prune=now");
        var indexPath = Assert.Single(Directory.GetFiles(
            Path.Combine(directory.Path, ".git", "objects", "pack"), "*.idx"));
        using var index = await GitPackIndex.OpenAsync(
            indexPath, GitObjectFormat.Sha1, CancellationToken.None);
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => index.TryFindOffset(
            GitObjectId.Parse("0000000000000000000000000000000000000000"),
            source.Token));
    }
}
