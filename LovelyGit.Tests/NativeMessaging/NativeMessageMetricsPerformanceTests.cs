using System.Diagnostics;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using Xunit.Abstractions;

namespace LovelyGit.Tests.NativeMessaging;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeMessageMetricsPerformanceTests
{
    private const int Iterations = 1_000;
    private readonly ITestOutputHelper _output;

    public NativeMessageMetricsPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Create_ReusesOperatingSystemMemorySample()
    {
        _ = NativeMessageMetricsFactory.Create(Stopwatch.GetTimestamp(), 0);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var startedAt = Stopwatch.GetTimestamp();
        NativeMessageMetrics? last = null;

        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            last = NativeMessageMetricsFactory.Create(startedAt, iteration);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var bytesPerSample = allocated / Iterations;
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        _output.WriteLine(
            $"{Iterations:N0} samples: {elapsed.TotalMilliseconds:N2} ms, " +
            $"{bytesPerSample:N0} bytes/sample");
        Assert.NotNull(last);
        Assert.True(
            bytesPerSample < 256,
            $"Metrics allocated {bytesPerSample:N0} bytes/sample ({allocated:N0} total).");
    }
}
