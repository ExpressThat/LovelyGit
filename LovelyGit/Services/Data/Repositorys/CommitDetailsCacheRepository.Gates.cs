namespace ExpressThat.LovelyGit.Services.Data.Repositorys;

internal sealed partial class CommitDetailsCacheRepository
{
    private SaveGate GetSaveGate(string key)
    {
        lock (_saveGateLock)
        {
            if (!_saveGates.TryGetValue(key, out var gate))
            {
                gate = new SaveGate();
                _saveGates[key] = gate;
            }

            gate.ReferenceCount++;
            return gate;
        }
    }

    private void ReleaseSaveGate(string key, SaveGate gate)
    {
        lock (_saveGateLock)
        {
            gate.ReferenceCount--;
            if (gate.ReferenceCount == 0 &&
                _saveGates.TryGetValue(key, out var activeGate) &&
                ReferenceEquals(activeGate, gate))
            {
                _saveGates.Remove(key);
                gate.Semaphore.Dispose();
            }
        }
    }

    private sealed class SaveGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }
    }
}
