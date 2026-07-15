using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.RemoteSync;
using LovelyGit.Tests.Git.Branches;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.RemoteSync;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeRemoteSyncPerformanceTests(ITestOutputHelper output)
{
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
}
