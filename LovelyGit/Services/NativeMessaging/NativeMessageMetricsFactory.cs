using System.Diagnostics;
using System.Text;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal static class NativeMessageMetricsFactory
{
    public static NativeMessageMetrics Create(
        long startedAt,
        int requestPayloadBytes)
    {
        var process = Process.GetCurrentProcess();
        return new NativeMessageMetrics(
            GetElapsedMilliseconds(startedAt),
            requestPayloadBytes,
            Math.Max(0, GC.GetTotalMemory(false)),
            Math.Max(0, process.WorkingSet64),
            Math.Max(0, process.PrivateMemorySize64));
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
}
