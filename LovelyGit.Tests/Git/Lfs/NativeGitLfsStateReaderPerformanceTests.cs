using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Lfs;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Lfs;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeGitLfsStateReaderPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ReadPatternsAsync_ScansLargeAttributesFileWithinManagerBudget()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-lfs-performance-");
        try
        {
            await WriteAttributesAsync(directory.FullName, 300_000);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var patterns = await NativeGitLfsStateReader.ReadPatternsAsync(
                directory.FullName, CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.Equal(10_000, patterns.Count);
            Assert.Equal("asset-000000.bin", patterns[0]);
            Assert.Equal("asset-299970.bin", patterns[^1]);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(120), $"Scan took {elapsed}.");
            Assert.True(allocated < 5_000_000, $"Scan allocated {allocated:N0} bytes.");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static async Task WriteAttributesAsync(string path, int lineCount)
    {
        await using var stream = new FileStream(
            Path.Combine(path, ".gitattributes"), FileMode.CreateNew, FileAccess.Write);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        for (var index = 0; index < lineCount; index++)
        {
            if (index % 30 == 0)
            {
                await writer.WriteLineAsync($"asset-{index:D6}.bin filter=lfs diff=lfs -text");
            }
            else
            {
                await writer.WriteLineAsync($"source-{index:D6}.txt text eol=lf");
            }
        }
    }
}
