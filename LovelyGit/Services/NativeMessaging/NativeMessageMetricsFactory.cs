using System.Diagnostics;
using System.Text;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal static class NativeMessageMetricsFactory
{
    private static readonly TimeSpan MemorySampleInterval = TimeSpan.FromSeconds(1);
    private static readonly object MemorySampleSync = new();
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();
    private static long sampledAt;
    private static long sampledWorkingSet;
    private static long sampledPrivateMemory;

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
        var now = Stopwatch.GetTimestamp();
        var lastSample = Volatile.Read(ref sampledAt);
        if (lastSample != 0 && Stopwatch.GetElapsedTime(lastSample, now) < MemorySampleInterval)
        {
            return ReadCachedSample();
        }

        lock (MemorySampleSync)
        {
            lastSample = Volatile.Read(ref sampledAt);
            if (lastSample != 0 && Stopwatch.GetElapsedTime(lastSample, now) < MemorySampleInterval)
            {
                return ReadCachedSample();
            }

            CurrentProcess.Refresh();
            Volatile.Write(ref sampledWorkingSet, Math.Max(0, CurrentProcess.WorkingSet64));
            Volatile.Write(ref sampledPrivateMemory, Math.Max(0, CurrentProcess.PrivateMemorySize64));
            Volatile.Write(ref sampledAt, now);
            return ReadCachedSample();
        }
    }

    private static ProcessMemorySample ReadCachedSample() =>
        new(
            Volatile.Read(ref sampledWorkingSet),
            Volatile.Read(ref sampledPrivateMemory));

    private readonly record struct ProcessMemorySample(long WorkingSet, long PrivateMemory);
}
