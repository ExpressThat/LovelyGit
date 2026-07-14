using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(PerformanceTestCollection.Name)]
public sealed class PorcelainStatusCounterTests(ITestOutputHelper output)
{
    [Fact]
    public void Write_CountsRecordsAndRenamePairsAcrossChunkBoundaries()
    {
        var counter = new PorcelainStatusCounter();
        var bytes = Encoding.UTF8.GetBytes(
            " M changed.txt\0R  renamed.txt\0old.txt\0?? new.txt\0");

        foreach (var value in bytes)
        {
            counter.Write([value]);
        }

        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task WriteAsync_CancellationDoesNotConsumeInput()
    {
        var counter = new PorcelainStatusCounter();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await counter.WriteAsync("?? file.txt\0"u8.ToArray(), cancellation.Token));

        Assert.Equal(0, counter.Count);
    }

    [Fact]
    public void CountWideStatus_HasConstantMemory()
    {
        const int recordCount = 100_000;
        var counter = new PorcelainStatusCounter();
        ReadOnlySpan<byte> record = "?? path/to/file.txt\0"u8;
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var startedAt = Stopwatch.GetTimestamp();

        for (var index = 0; index < recordCount; index++)
        {
            counter.Write(record);
        }

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        output.WriteLine(
            $"{recordCount:N0} streamed status records: " +
            $"{elapsed.TotalMilliseconds:N1} ms; {allocated:N0} bytes");
        Assert.Equal(recordCount, counter.Count);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(500), $"Counting took {elapsed}.");
        Assert.True(allocated < 100_000, $"Counting allocated {allocated:N0} bytes.");
    }
}
