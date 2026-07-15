using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Patches;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Patches;

[Collection(PerformanceTestCollection.Name)]
public sealed class PatchPreviewPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ReadAsync_PreviewsLargePatchWithinInteractionBudget()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lovelygit-preview-{Guid.NewGuid():N}.patch");
        try
        {
            await WritePatchAsync(path, 300_000);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var preview = await PatchPreviewService.ReadAsync(path, CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.Single(preview.Files);
            Assert.Equal(300_000, preview.TotalAdditions);
            Assert.Equal(300_000, preview.TotalDeletions);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(150), $"Preview took {elapsed}.");
            Assert.True(allocated < 2_000_000, $"Preview allocated {allocated:N0} bytes.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task WritePatchAsync(string path, int changedLines)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteLineAsync("--- a/large.txt");
        await writer.WriteLineAsync("+++ b/large.txt");
        await writer.WriteLineAsync($"@@ -1,{changedLines} +1,{changedLines} @@");
        for (var index = 0; index < changedLines; index++)
        {
            await writer.WriteLineAsync($"-old line {index:D6}");
            await writer.WriteLineAsync($"+new line {index:D6}");
        }
    }
}
