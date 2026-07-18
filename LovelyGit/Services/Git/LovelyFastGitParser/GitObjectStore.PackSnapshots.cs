using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class GitObjectStore
{
    private static readonly long PackValidationIntervalTicks =
        Math.Max(1, Stopwatch.Frequency / 40);
    private static long _nextPackIndexGeneration;
    private int _openPackIndexCount;

    internal int OpenPackIndexCount => Volatile.Read(ref _openPackIndexCount);

    private void ReleasePackIndexes(PackIndexSnapshot snapshot)
    {
        lock (_packIndexSnapshotsGate)
        {
            snapshot.ActiveReaders--;
            if (snapshot.Retired && snapshot.ActiveReaders == 0)
            {
                DisposePackIndexesCore(snapshot);
            }
        }
    }

    private void RetirePackIndexesCore(PackIndexSnapshot snapshot)
    {
        snapshot.Retired = true;
        if (snapshot.ActiveReaders == 0)
        {
            DisposePackIndexesCore(snapshot);
        }
    }

    private void DisposePackIndexesCore(PackIndexSnapshot snapshot)
    {
        if (snapshot.Disposed) return;
        snapshot.Disposed = true;
        _packReader.ClearObjectCache();
        foreach (var index in snapshot.Indexes)
        {
            _packReader.RetirePackFile(index.PackPath);
            index.Dispose();
        }
        Interlocked.Add(ref _openPackIndexCount, -snapshot.Indexes.Count);
    }

    private sealed class PackIndexSnapshot(long generation, IReadOnlyList<GitPackIndex> indexes)
    {
        private long _nextValidationAt;

        public long Generation { get; } = generation;
        public IReadOnlyList<GitPackIndex> Indexes { get; } = indexes;
        public int ActiveReaders { get; set; }
        public bool Disposed { get; set; }
        public bool Retired { get; set; }

        public bool TryBeginValidation()
        {
            var now = Stopwatch.GetTimestamp();
            var next = Volatile.Read(ref _nextValidationAt);
            return now >= next
                && Interlocked.CompareExchange(
                    ref _nextValidationAt,
                    now + PackValidationIntervalTicks,
                    next) == next;
        }
    }

    private readonly struct PackIndexLease(
        GitObjectStore owner,
        PackIndexSnapshot snapshot) : IDisposable
    {
        public long Generation => snapshot.Generation;
        public IReadOnlyList<GitPackIndex> Indexes => snapshot.Indexes;
        public bool TryBeginValidation() => snapshot.TryBeginValidation();

        public void Dispose() => owner.ReleasePackIndexes(snapshot);
    }
}
