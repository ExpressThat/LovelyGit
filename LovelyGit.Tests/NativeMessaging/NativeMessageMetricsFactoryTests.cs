using System.Diagnostics;
using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class NativeMessageMetricsFactoryTests
{
    [Fact]
    public void CountUtf8Bytes_ReturnsZero_WhenPayloadIsMissing()
    {
        Assert.Equal(0, NativeMessageMetricsFactory.CountUtf8Bytes(null));
        Assert.Equal(0, NativeMessageMetricsFactory.CountUtf8Bytes(string.Empty));
    }

    [Fact]
    public void CountUtf8Bytes_UsesUtf8Encoding()
    {
        Assert.Equal(6, NativeMessageMetricsFactory.CountUtf8Bytes("abc-£"));
    }

    [Fact]
    public void Create_ReturnsNonNegativeMetrics()
    {
        var metrics = NativeMessageMetricsFactory.Create(Stopwatch.GetTimestamp(), 42);

        Assert.True(metrics.DurationMs >= 0);
        Assert.Equal(42, metrics.RequestPayloadBytes);
        Assert.True(metrics.ManagedMemoryBytes >= 0);
        Assert.True(metrics.WorkingSetBytes >= 0);
        Assert.True(metrics.PrivateMemoryBytes >= 0);
    }
}
