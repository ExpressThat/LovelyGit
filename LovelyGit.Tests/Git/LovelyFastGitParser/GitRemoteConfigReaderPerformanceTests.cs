using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

[Collection(PerformanceTestCollection.Name)]
public sealed class GitRemoteConfigReaderPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ReadPrimaryRemoteUrlAsync_HandlesLargeConfigWithinRefreshBudget()
    {
        using var fixture = await RemoteConfigFixture.CreateAsync(10_000);
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();

        string? url = null;
        for (var iteration = 0; iteration < 5; iteration++)
        {
            url = await GitRemoteConfigReader.ReadPrimaryRemoteUrlAsync(
                fixture.Path, CancellationToken.None);
        }

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        output.WriteLine($"PrimaryElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
        Assert.Equal("https://example.invalid/origin.git", url);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(50), $"Reads took {elapsed}.");
        Assert.True(allocated < 1_000_000, $"Reads allocated {allocated:N0} bytes.");
    }

    [Fact]
    public async Task ReadRemotesAsync_HandlesLargeConfigWithinManagerBudget()
    {
        using var fixture = await RemoteConfigFixture.CreateAsync(10_000);
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();

        var remotes = await GitRemoteConfigReader.ReadRemotesAsync(
            fixture.Path, CancellationToken.None);

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        output.WriteLine($"ManagerElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
        Assert.Equal(10_001, remotes.Count);
        Assert.Equal("origin", remotes[0].Name);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(40), $"Read took {elapsed}.");
        Assert.True(allocated < 12_000_000, $"Read allocated {allocated:N0} bytes.");
    }

    [Fact]
    public async Task ReadRemoteAsync_DoesNotMaterializeUnrelatedLargeConfigEntries()
    {
        using var fixture = await RemoteConfigFixture.CreateAsync(10_000);
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();

        var remote = await GitRemoteConfigReader.ReadRemoteAsync(
            fixture.Path, "remote-09999", CancellationToken.None);

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        output.WriteLine($"TargetElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
        Assert.NotNull(remote);
        Assert.Equal("https://example.invalid/09999.git", remote.Url);
        Assert.Equal("ssh://example.invalid/09999.git", remote.PushUrl);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(25), $"Read took {elapsed}.");
        Assert.True(allocated < 250_000, $"Read allocated {allocated:N0} bytes.");
    }

    private sealed class RemoteConfigFixture : IDisposable
    {
        private readonly DirectoryInfo _directory =
            Directory.CreateTempSubdirectory("lovelygit-remote-config-performance-");

        public string Path => _directory.FullName;

        public static async Task<RemoteConfigFixture> CreateAsync(int extraRemoteCount)
        {
            var fixture = new RemoteConfigFixture();
            await using var stream = new FileStream(
                System.IO.Path.Combine(fixture.Path, "config"), FileMode.CreateNew, FileAccess.Write);
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteLineAsync("[core]");
            await writer.WriteLineAsync("\trepositoryformatversion = 0");
            await writer.WriteLineAsync("[remote \"origin\"]");
            await writer.WriteLineAsync("\turl = https://example.invalid/origin.git");
            for (var index = 0; index < extraRemoteCount; index++)
            {
                await writer.WriteLineAsync($"[remote \"remote-{index:D5}\"]");
                await writer.WriteLineAsync($"\turl = https://example.invalid/{index:D5}.git");
                await writer.WriteLineAsync($"\tpushurl = ssh://example.invalid/{index:D5}.git");
            }
            return fixture;
        }

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
