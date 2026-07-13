using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal abstract class ConflictReadTrace : IDisposable
{
    public static ConflictReadTrace Start(string path, bool ignoreWhitespace) =>
        LovelyGitTrace.Enabled
            ? new EnabledTrace(path, ignoreWhitespace)
            : DisabledTrace.Instance;

    public abstract void Mark(string stage);
    public abstract void Dispose();

    private sealed class EnabledTrace(string path, bool ignoreWhitespace) : ConflictReadTrace
    {
        private readonly long _startedAt = Stopwatch.GetTimestamp();
        private long _stageStartedAt = Stopwatch.GetTimestamp();
        private long _stageAllocated = GC.GetTotalAllocatedBytes(precise: false);

        public override void Mark(string stage)
        {
            var now = Stopwatch.GetTimestamp();
            var allocated = GC.GetTotalAllocatedBytes(precise: false);
            LovelyGitTrace.Write(
                $"conflict.read.{stage}",
                $"{Stopwatch.GetElapsedTime(_stageStartedAt, now).TotalMilliseconds:F1}ms " +
                $"allocated={allocated - _stageAllocated} path={path} ignore={ignoreWhitespace}");
            _stageStartedAt = now;
            _stageAllocated = allocated;
        }

        public override void Dispose()
        {
            LovelyGitTrace.Write(
                "conflict.read.total",
                $"{Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds:F1}ms path={path} ignore={ignoreWhitespace}");
        }
    }

    private sealed class DisabledTrace : ConflictReadTrace
    {
        public static readonly DisabledTrace Instance = new();
        public override void Mark(string stage) { }
        public override void Dispose() { }
    }
}
