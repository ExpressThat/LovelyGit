using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitObjectStoreCancellationTests
{
    [Fact]
    public async Task UncachedLooseObjectRead_PropagatesCancellation()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-object-cancellation-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        var content = Guid.NewGuid().ToString("N");
        var path = Path.Combine(directory.Path, "object.txt");
        await File.WriteAllTextAsync(path, content);
        var objectId = (await GitTestProcess.RunAsync(
            directory.Path,
            "hash-object",
            "-w",
            path)).Trim();
        using var store = new GitObjectStore(
            Path.Combine(directory.Path, ".git"),
            GitObjectFormat.Sha1);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await store.ReadObjectWithoutCachingAsync(
                GitObjectId.Parse(objectId),
                cancellation.Token));
    }
}
