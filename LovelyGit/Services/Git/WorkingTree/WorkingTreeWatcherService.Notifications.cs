using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Buffers;
using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService : IDisposable
{
    private void QueueInvalidation()
    {
        CancellationTokenSource cancellation;
        lock (_lock)
        {
            if (_disposed || _activeRepositoryId == null)
            {
                return;
            }

            _debounceCancellation?.Cancel();
            _debounceCancellation?.Dispose();
            _debounceCancellation = new CancellationTokenSource();
            cancellation = _debounceCancellation;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, cancellation.Token).ConfigureAwait(false);
                await SendInvalidationAsync(cancellation).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }, CancellationToken.None);
    }

    private async Task SendInvalidationAsync(CancellationTokenSource cancellation)
    {
        WorkingTreeChangedNotification notification;
        lock (_lock)
        {
            if (_disposed || _activeRepositoryId == null || !ReferenceEquals(_debounceCancellation, cancellation))
            {
                return;
            }

            notification = new WorkingTreeChangedNotification
            {
                Generation = unchecked(++_generation),
            };
        }

        _nativeMessaging.Send(
            NativeMessageType.WorkingTreeChanged,
            notification,
            NativeMessagingJsonContext.Default.NativeMessageResponseWorkingTreeChangedNotification);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private void QueueGraphInvalidation()
    {
        CancellationTokenSource cancellation;
        lock (_lock)
        {
            if (_disposed || _activeRepositoryId == null)
            {
                return;
            }

            _graphDebounceCancellation?.Cancel();
            _graphDebounceCancellation?.Dispose();
            _graphDebounceCancellation = new CancellationTokenSource();
            cancellation = _graphDebounceCancellation;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, cancellation.Token).ConfigureAwait(false);
                await SendGraphInvalidationAsync(cancellation).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }, CancellationToken.None);
    }

    private async Task SendGraphInvalidationAsync(CancellationTokenSource cancellation)
    {
        string? gitDirectory;
        lock (_lock)
        {
            if (_disposed || _activeRepositoryId == null || !ReferenceEquals(_graphDebounceCancellation, cancellation))
            {
                return;
            }

            gitDirectory = _activeGitDirectory;
        }

        if (string.IsNullOrEmpty(gitDirectory))
        {
            return;
        }

        var nextSnapshot = ComputeCommitGraphSnapshot(gitDirectory);
        CommitGraphChangedNotification notification;
        lock (_lock)
        {
            if (_disposed || _activeRepositoryId == null || !ReferenceEquals(_graphDebounceCancellation, cancellation))
            {
                return;
            }

            if (_commitGraphSnapshot == nextSnapshot)
            {
                return;
            }

            _commitGraphSnapshot = nextSnapshot;
            notification = new CommitGraphChangedNotification
            {
                Generation = unchecked(++_graphGeneration),
            };
        }

        _nativeMessaging.Send(
            NativeMessageType.CommitGraphChanged,
            notification,
            NativeMessagingJsonContext.Default.NativeMessageResponseCommitGraphChangedNotification);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private void StopActiveWatchers()
    {
        lock (_lock)
        {
            StopActiveWatchersCore();
        }
    }

    private void StopActiveWatchersCore()
    {
        _debounceCancellation?.Cancel();
        _debounceCancellation?.Dispose();
        _debounceCancellation = null;
        _graphDebounceCancellation?.Cancel();
        _graphDebounceCancellation?.Dispose();
        _graphDebounceCancellation = null;
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnFileChanged;
            watcher.Created -= OnFileChanged;
            watcher.Deleted -= OnFileChanged;
            watcher.Renamed -= OnFileChanged;
            watcher.Error -= OnWatcherError;
            watcher.Dispose();
        }

        _watchers.Clear();
        _activeRepositoryId = null;
        _activeRepositoryPath = null;
        _activeGitDirectory = null;
        _activeWorkTreeDirectory = null;
        _ignoreMatcher = null;
        _commitGraphSnapshot = null;
    }

}
