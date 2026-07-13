using System.Diagnostics;
using System.Text;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal static class NativeMessageMetricsFactory
{
    private static readonly TimeSpan MemorySampleInterval = TimeSpan.FromSeconds(1);
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();
    private static readonly NativeMessageMemorySampler MemorySampler = new(
        ReadProcessMemory,
        MemorySampleInterval);

    public static NativeMessageMetrics Create(
        long startedAt,
        int requestPayloadBytes)
    {
        var memory = GetProcessMemorySample();
        return new NativeMessageMetrics(
            GetElapsedMilliseconds(startedAt),
            requestPayloadBytes,
            Math.Max(0, GC.GetTotalMemory(false)),
            memory.WorkingSet,
            memory.PrivateMemory);
    }

    public static int CountUtf8Bytes(string? value)
    {
        return string.IsNullOrEmpty(value)
            ? 0
            : Encoding.UTF8.GetByteCount(value);
    }

    private static long GetElapsedMilliseconds(long startedAt)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        return Math.Max(0, (long)Math.Ceiling(elapsed.TotalMilliseconds));
    }

    private static ProcessMemorySample GetProcessMemorySample()
    {
        var sample = MemorySampler.GetSample();
        return new ProcessMemorySample(sample.WorkingSet, sample.PrivateMemory);
    }

    private static NativeMessageMemorySample ReadProcessMemory()
    {
        CurrentProcess.Refresh();
        return new NativeMessageMemorySample(
            CurrentProcess.WorkingSet64,
            CurrentProcess.PrivateMemorySize64);
    }

    private readonly record struct ProcessMemorySample(long WorkingSet, long PrivateMemory);
}
