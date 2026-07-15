using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.RemoteSync;
using LovelyGit.Tests.Git.Branches;
using LovelyGit.Tests.Git.Cli;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.RemoteSync;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeRemoteSyncPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task UpstreamRefresh_DoesNotScaleWithUnrelatedRefs()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var head = repository.Head(repository.ClonePath);
        SeedUnrelatedRefs(repository.ClonePath, head, 1_500);
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();

        var response = await NativeRemoteSyncStatusReader.ReadAsync(
            repository.ClonePath, CancellationToken.None);

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
        Assert.True(response.HasUpstream);
        Assert.True(response.IsUpstreamAvailable);
        Assert.Equal(0, response.AheadCount);
        Assert.Equal(0, response.BehindCount);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(100), $"Refresh took {elapsed}.");
        Assert.True(allocated < 500_000, $"Refresh allocated {allocated:N0} bytes.");
    }

    [Fact]
    public async Task NoUpstreamRefresh_ReusesTheHeadSnapshot()
    {
        using var repository = TemporaryGitRepository.Create();
        await NativeRemoteSyncStatusReader.ReadAsync(repository.Path, CancellationToken.None);
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();
        for (var iteration = 0; iteration < 100; iteration++)
        {
            var response = await NativeRemoteSyncStatusReader.ReadAsync(
                repository.Path, CancellationToken.None);
            Assert.False(response.HasUpstream);
            Assert.Equal("master", response.BranchName);
            Assert.NotNull(response.LocalHash);
        }

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}");
        output.WriteLine($"AllocatedBytes={allocated:N0}");
        Assert.True(elapsed < TimeSpan.FromMilliseconds(150), $"Refreshes took {elapsed}.");
        Assert.True(allocated < 4_000_000, $"Refreshes allocated {allocated:N0} bytes.");
    }

    private static void SeedUnrelatedRefs(string repositoryPath, string commit, int count)
    {
        var heads = Directory.CreateDirectory(
            Path.Combine(repositoryPath, ".git", "refs", "heads", "perf"));
        for (var index = 0; index < count; index++)
        {
            File.WriteAllText(Path.Combine(heads.FullName, $"branch-{index:D4}"), commit + "\n");
        }
    }
}
