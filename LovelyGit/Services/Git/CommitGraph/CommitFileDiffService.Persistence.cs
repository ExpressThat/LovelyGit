using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    private const int MaxPendingDiffs = 8;
    private readonly object _pendingDiffsLock = new();
    private readonly Dictionary<string, PendingDiff> _pendingDiffs = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _persistenceGate = new(1, 1);

    private bool TryGetPendingDiff(string key, out CommitFileDiffResponse response)
    {
        lock (_pendingDiffsLock)
        {
            if (_pendingDiffs.TryGetValue(key, out var pending))
            {
                response = pending.Response;
                return true;
            }
        }

        response = null!;
        return false;
    }

    private void QueueDiffPersistence(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitFileDiffResponse response,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = MakeDiffGateKey(
            repositoryId,
            commitHash,
            path,
            response.ViewMode,
            ignoreWhitespace);
        var pending = new PendingDiff(response);
        lock (_pendingDiffsLock)
        {
            if (_pendingDiffs.ContainsKey(key) || _pendingDiffs.Count >= MaxPendingDiffs)
            {
                return;
            }

            _pendingDiffs.Add(key, pending);
        }

        _ = PersistPendingDiffAsync(
            repositoryId,
            commitHash,
            path,
            ignoreWhitespace,
            key,
            pending);
    }

    private async Task PersistPendingDiffAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        bool ignoreWhitespace,
        string key,
        PendingDiff pending)
    {
        await Task.Yield();
        try
        {
            await SavePersistentDiffAsync(
                    repositoryId,
                    commitHash,
                    path,
                    pending.Response,
                    ignoreWhitespace,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            lock (_pendingDiffsLock)
            {
                if (_pendingDiffs.TryGetValue(key, out var active)
                    && ReferenceEquals(active, pending))
                {
                    _pendingDiffs.Remove(key);
                }
            }
        }
    }

    private async Task SavePersistentDiffAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitFileDiffResponse response,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        await _persistenceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _persistentCache!.SaveAsync(
                    repositoryId,
                    commitHash,
                    path,
                    response,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _persistenceGate.Release();
        }
    }

    private async Task RemovePersistentDiffAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        await _persistenceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _persistentCache!.RemoveAsync(
                    repositoryId,
                    commitHash,
                    path,
                    viewMode,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _persistenceGate.Release();
        }
    }

    private async Task ClearPersistentDiffsAsync(
        Guid repositoryId,
        string commitHash,
        CancellationToken cancellationToken)
    {
        await _persistenceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _persistentCache!.ClearAsync(repositoryId, commitHash, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _persistenceGate.Release();
        }
    }

    private sealed record PendingDiff(CommitFileDiffResponse Response);
}
