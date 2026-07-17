using System.Buffers.Binary;
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

    [Fact]
    public async Task TryFindOffset_LargeIndexUsesAllocationFreeDirectReads()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-large-pack-index-");
        var indexPath = Path.Combine(directory.Path, "pack-large.idx");
        var header = new byte[8 + (256 * 4) + 20 + 4 + 4];
        header[0] = 0xff;
        header[1] = 0x74;
        header[2] = 0x4f;
        header[3] = 0x63;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), 2);
        for (var index = 0; index < 256; index++)
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8 + (index * 4)), 1);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(header.Length - 4), 12);
        await using (var file = new FileStream(indexPath, FileMode.CreateNew, FileAccess.Write))
        {
            await file.WriteAsync(header);
            file.SetLength(129L * 1024 * 1024);
        }

        var cachedBefore = GitPackIndex.CachedIndexBytes;
        using var packIndex = await GitPackIndex.OpenAsync(
            indexPath,
            GitObjectFormat.Sha1,
            CancellationToken.None);

        var offset = packIndex.TryFindOffset(
            GitObjectId.Parse("0000000000000000000000000000000000000000"),
            CancellationToken.None);

        Assert.Equal(12, offset);
        Assert.Equal(cachedBefore, GitPackIndex.CachedIndexBytes);
    }
}
