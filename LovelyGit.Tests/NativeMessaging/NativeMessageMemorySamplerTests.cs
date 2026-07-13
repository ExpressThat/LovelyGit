using System.Diagnostics;
using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class NativeMessageMemorySamplerTests
{
    [Fact]
    public void GetSample_DoesNotWaitForSlowOperatingSystemRead()
    {
        using var readStarted = new ManualResetEventSlim();
        using var releaseRead = new ManualResetEventSlim();
        var sampler = new NativeMessageMemorySampler(
            () =>
            {
                readStarted.Set();
                releaseRead.Wait(TimeSpan.FromSeconds(5));
                return new NativeMessageMemorySample(12, 34);
            },
            TimeSpan.FromMinutes(1));

        var startedAt = Stopwatch.GetTimestamp();
        var sample = sampler.GetSample();
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        Assert.Equal(default, sample);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(100), $"Sampling blocked for {elapsed}.");
        Assert.True(readStarted.Wait(TimeSpan.FromSeconds(1)));
        releaseRead.Set();
        AssertEventually(() => sampler.GetSample() == new NativeMessageMemorySample(12, 34));
    }

    [Fact]
    public void GetSample_SwallowsTelemetryFailuresAndCanRetryLater()
    {
        var attempts = 0;
        var sampler = new NativeMessageMemorySampler(
            () =>
            {
                if (Interlocked.Increment(ref attempts) == 1) throw new InvalidOperationException("sample failed");
                return new NativeMessageMemorySample(56, 78);
            },
            TimeSpan.Zero);

        _ = sampler.GetSample();
        AssertEventually(() => Volatile.Read(ref attempts) >= 1);
        AssertEventually(() => sampler.GetSample() == new NativeMessageMemorySample(56, 78));
    }

    private static void AssertEventually(Func<bool> assertion)
    {
        Assert.True(SpinWait.SpinUntil(assertion, TimeSpan.FromSeconds(2)));
    }
}
