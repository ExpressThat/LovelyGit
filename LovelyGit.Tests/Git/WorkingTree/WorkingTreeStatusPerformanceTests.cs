using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(PerformanceTestCollection.Name)]
public sealed class WorkingTreeStatusPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task NativeStatus_ScansLargeIndexWithoutForcingFullCollection()
    {
        const int entryCount = 10_000;
        using var directory = TemporaryDirectory.Create("lovelygit-status-performance-");
        var gitDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, ".git"));
        SyntheticGitIndexWriter.WriteVersion2(
            Path.Combine(gitDirectory.FullName, "index"),
            entryCount);
        var service = new WorkingTreeStatusListService(new GitCliService());

        var collectionsBefore = GC.CollectionCount(2);
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();
        var response = await service.GetChangesAsync(directory.Path, CancellationToken.None);
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        var collections = GC.CollectionCount(2) - collectionsBefore;

        output.WriteLine(
            $"10k status: {elapsed.TotalMilliseconds:N1} ms, " +
            $"{allocated:N0} bytes, {collections} gen-2 collections");
        Assert.Equal(entryCount, response.Unstaged.Count);
        Assert.Equal(0, collections);
        Assert.True(elapsed < TimeSpan.FromSeconds(1), $"Status scan took {elapsed}.");
        Assert.True(allocated < 20_000_000, $"Status scan allocated {allocated:N0} bytes.");
    }
}
