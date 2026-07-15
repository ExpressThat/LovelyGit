using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

[Collection(PerformanceTestCollection.Name)]
public sealed class GitStashReaderPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ReadAsync_LoadsLargeReflogWithinRefreshBudget()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-stash-performance-");
        try
        {
            await WriteReflogAsync(directory.FullName, 100_000);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var entries = await GitStashReader.ReadAsync(
                directory.FullName, GitObjectFormat.Sha1, CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.Equal(100_000, entries.Count);
            Assert.Equal("stash@{0}", entries[0].Selector);
            Assert.Equal("stash message 099999", entries[0].Message);
            Assert.Equal("stash@{99999}", entries[^1].Selector);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(200), $"Read took {elapsed}.");
            Assert.True(allocated < 35_000_000, $"Read allocated {allocated:N0} bytes.");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static async Task WriteReflogAsync(string gitDirectory, int count)
    {
        var directory = Directory.CreateDirectory(Path.Combine(gitDirectory, "logs", "refs"));
        await using var stream = new FileStream(
            Path.Combine(directory.FullName, "stash"), FileMode.CreateNew, FileAccess.Write);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        const string OldHash = "1111111111111111111111111111111111111111";
        const string NewHash = "2222222222222222222222222222222222222222";
        for (var index = 0; index < count; index++)
        {
            await writer.WriteAsync(OldHash);
            await writer.WriteAsync(' ');
            await writer.WriteAsync(NewHash);
            await writer.WriteAsync(" User <user@example.invalid> ");
            await writer.WriteAsync((1_700_000_000 + index).ToString());
            await writer.WriteAsync(" +0000\tstash message ");
            await writer.WriteLineAsync(index.ToString("D6"));
        }
    }
}
