using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreePreliminarySummaryCacheTests
{
    [Fact]
    public async Task GetSummaryAsync_RootChangeInvalidatesCachedCount()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-summary-cache-");
        await CreateInitialCommitAsync(directory.Path);
        var service = new WorkingTreePreliminarySummaryService();

        var before = await service.GetSummaryAsync(
            directory.Path,
            Path.Combine(directory.Path, ".git"),
            CancellationToken.None);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "untracked.txt"), "new");
        var after = await service.GetSummaryAsync(
            directory.Path,
            Path.Combine(directory.Path, ".git"),
            CancellationToken.None);

        Assert.Equal(0, before.TotalCount);
        Assert.Equal(1, after.TotalCount);
    }

    [Fact]
    public async Task GetSummaryAsync_IndexChangeInvalidatesCachedCount()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-summary-cache-");
        await CreateInitialCommitAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "tracked-next.txt"), "new");
        var service = new WorkingTreePreliminarySummaryService();

        var before = await service.GetSummaryAsync(
            directory.Path,
            Path.Combine(directory.Path, ".git"),
            CancellationToken.None);
        await GitTestProcess.RunAsync(directory.Path, "add", "tracked-next.txt");
        var after = await service.GetSummaryAsync(
            directory.Path,
            Path.Combine(directory.Path, ".git"),
            CancellationToken.None);

        Assert.Equal(1, before.TotalCount);
        Assert.Equal(0, after.TotalCount);
    }

    private static async Task CreateInitialCommitAsync(string path)
    {
        await GitTestProcess.RunAsync(path, "init");
        await GitTestProcess.RunAsync(path, "config", "user.email", "test@example.com");
        await GitTestProcess.RunAsync(path, "config", "user.name", "Test User");
        await File.WriteAllTextAsync(Path.Combine(path, "file.txt"), "hello");
        await GitTestProcess.RunAsync(path, "add", ".");
        await GitTestProcess.RunAsync(path, "commit", "-m", "initial");
    }
}
