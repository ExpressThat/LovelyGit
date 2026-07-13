namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitDetailsService
{
    private readonly object _commitBuildGateLock = new();
    private readonly Dictionary<string, BuildGate> _commitBuildGates = new(StringComparer.Ordinal);

    private BuildGate GetBuildGate(string key)
    {
        lock (_commitBuildGateLock)
        {
            if (!_commitBuildGates.TryGetValue(key, out var gate))
            {
                gate = new BuildGate();
                _commitBuildGates[key] = gate;
            }

            gate.ReferenceCount++;
            return gate;
        }
    }

    private void ReleaseBuildGate(string key, BuildGate gate)
    {
        lock (_commitBuildGateLock)
        {
            gate.ReferenceCount--;
            if (gate.ReferenceCount == 0
                && _commitBuildGates.TryGetValue(key, out var activeGate)
                && ReferenceEquals(activeGate, gate))
            {
                _commitBuildGates.Remove(key);
                gate.Semaphore.Dispose();
            }
        }
    }

    private sealed class BuildGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }
    }
}
