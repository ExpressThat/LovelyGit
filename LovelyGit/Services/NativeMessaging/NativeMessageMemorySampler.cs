using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal sealed class NativeMessageMemorySampler(
    Func<NativeMessageMemorySample> readMemory,
    TimeSpan sampleInterval)
{
    private readonly Func<NativeMessageMemorySample> _readMemory = readMemory;
    private readonly TimeSpan _sampleInterval = sampleInterval;
    private long _sampledAt;
    private long _workingSet;
    private long _privateMemory;
    private int _sampleQueued;

    public NativeMessageMemorySample GetSample()
    {
        QueueRefreshIfStale();
        return new NativeMessageMemorySample(
            Volatile.Read(ref _workingSet),
            Volatile.Read(ref _privateMemory));
    }

    private void QueueRefreshIfStale()
    {
        var sampledAt = Volatile.Read(ref _sampledAt);
        if (sampledAt != 0 &&
            Stopwatch.GetElapsedTime(sampledAt, Stopwatch.GetTimestamp()) < _sampleInterval)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _sampleQueued, 1, 0) != 0) return;
        ThreadPool.UnsafeQueueUserWorkItem(
            static sampler => sampler.Refresh(),
            this,
            preferLocal: false);
    }

    private void Refresh()
    {
        try
        {
            var sample = _readMemory();
            Volatile.Write(ref _workingSet, Math.Max(0, sample.WorkingSet));
            Volatile.Write(ref _privateMemory, Math.Max(0, sample.PrivateMemory));
        }
        catch
        {
            // Metrics must never affect a user operation.
        }
        finally
        {
            Volatile.Write(ref _sampledAt, Stopwatch.GetTimestamp());
            Volatile.Write(ref _sampleQueued, 0);
        }
    }
}

internal readonly record struct NativeMessageMemorySample(long WorkingSet, long PrivateMemory);
