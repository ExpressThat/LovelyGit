using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitDetailsService
{
    private const int MaxPendingDetails = 8;
    private readonly object _pendingDetailsLock = new();
    private readonly Dictionary<string, PendingDetails> _pendingDetails = new(StringComparer.Ordinal);

    private bool TryGetPendingDetails(
        Guid repositoryId,
        string commitHash,
        out CommitDetailsResponse details)
    {
        var key = MakeGateKey(repositoryId, commitHash);
        lock (_pendingDetailsLock)
        {
            if (_pendingDetails.TryGetValue(key, out var pending))
            {
                details = pending.Details;
                return true;
            }
        }

        details = null!;
        return false;
    }

    private async Task PersistOrQueueAsync(
        Guid repositoryId,
        string commitHash,
        CommitDetailsResponse details,
        bool waitForPersistence,
        CancellationToken cancellationToken)
    {
        if (waitForPersistence)
        {
            await _saveDetailsAsync(repositoryId, commitHash, details, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var key = MakeGateKey(repositoryId, commitHash);
        var pending = new PendingDetails(details);
        lock (_pendingDetailsLock)
        {
            if (_pendingDetails.Count >= MaxPendingDetails)
            {
                return;
            }

            _pendingDetails[key] = pending;
        }

        _ = PersistPendingAsync(repositoryId, commitHash, key, pending);
    }

    private async Task PersistPendingAsync(
        Guid repositoryId,
        string commitHash,
        string key,
        PendingDetails pending)
    {
        try
        {
            await _saveDetailsAsync(
                    repositoryId,
                    commitHash,
                    pending.Details,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            lock (_pendingDetailsLock)
            {
                if (_pendingDetails.TryGetValue(key, out var active)
                    && ReferenceEquals(active, pending))
                {
                    _pendingDetails.Remove(key);
                }
            }
        }
    }

    private sealed record PendingDetails(CommitDetailsResponse Details);
}
