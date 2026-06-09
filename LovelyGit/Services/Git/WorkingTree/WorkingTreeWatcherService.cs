using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class WorkingTreeWatcherService : IDisposable
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(200);
    private readonly IHubContext<CommsHub> _hubContext;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly object _lock = new();
    private readonly List<FileSystemWatcher> _watchers = new();
    private CancellationTokenSource? _debounceCancellation;
    private CancellationTokenSource? _graphDebounceCancellation;
    private Guid? _activeRepositoryId;
    private string? _activeRepositoryPath;
    private string? _activeGitDirectory;
    private string? _activeWorkTreeDirectory;
    private GitIgnoreMatcher? _ignoreMatcher;
    private int _generation;
    private int _graphGeneration;
    private bool _disposed;

    public WorkingTreeWatcherService(
        IHubContext<CommsHub> hubContext,
        KnownGitRepositorysRepository knownGitRepositorysRepository)
    {
        _hubContext = hubContext;
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
    }

    public async Task SwitchActiveRepositoryAsync(Guid? repositoryId)
    {
        if (repositoryId == null || repositoryId.Value == Guid.Empty)
        {
            StopActiveWatchers();
            return;
        }

        KnownGitRepository repository;
        try
        {
            repository = await _knownGitRepositorysRepository.FindByIdAsync(repositoryId.Value)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Trace.TraceWarning("Working tree watcher could not find active repository: {0}", exception);
            StopActiveWatchers();
            return;
        }

        if (string.IsNullOrWhiteSpace(repository.Path))
        {
            StopActiveWatchers();
            return;
        }

        await SwitchActiveRepositoryAsync(repository.Id, repository.Path).ConfigureAwait(false);
    }

    public async Task SwitchActiveRepositoryAsync(Guid repositoryId, string repositoryPath)
    {
        lock (_lock)
        {
            if (_activeRepositoryId == repositoryId
                && string.Equals(_activeRepositoryPath, repositoryPath, StringComparison.Ordinal))
            {
                return;
            }
        }

        GitRepositoryPaths paths;
        try
        {
            paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(repositoryPath, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Trace.TraceWarning("Working tree watcher could not open active repository: {0}", exception);
            StopActiveWatchers();
            return;
        }

        lock (_lock)
        {
            ThrowIfDisposed();
            StopActiveWatchersCore();
            _activeRepositoryId = repositoryId;
            _activeRepositoryPath = repositoryPath;
            _activeGitDirectory = paths.GitDirectory;
            _activeWorkTreeDirectory = paths.WorkTreeDirectory;
            _ignoreMatcher = null;

            AddWatcher(paths.WorkTreeDirectory, "*", includeSubdirectories: true);
            AddWatcher(paths.GitDirectory, "*", includeSubdirectories: false);
            var refsPath = Path.Combine(paths.GitDirectory, "refs");
            if (Directory.Exists(refsPath))
            {
                AddWatcher(refsPath, "*", includeSubdirectories: true);
            }

            var infoPath = Path.Combine(paths.GitDirectory, "info");
            if (Directory.Exists(infoPath))
            {
                AddWatcher(infoPath, "exclude", includeSubdirectories: false);
            }
        }

        _ = RefreshIgnoreMatcherAsync();
        QueueInvalidation();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopActiveWatchersCore();
        }
    }

    private void AddWatcher(string path, string filter, bool includeSubdirectories)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var watcher = new FileSystemWatcher(path, filter)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
                | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
        };
        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileChanged;
        watcher.Error += OnWatcherError;
        _watchers.Add(watcher);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (IsRelevantGitMetadataPath(eventArgs.FullPath))
        {
            QueueInvalidation();
            if (IsCommitGraphMetadataPath(eventArgs.FullPath))
            {
                QueueGraphInvalidation();
            }

            return;
        }

        if (IsIgnoredInternalPath(eventArgs.FullPath))
        {
            return;
        }

        if (IsIgnoreRulePath(eventArgs.FullPath))
        {
            _ = RefreshIgnoreMatcherAsync();
            QueueInvalidation();
            return;
        }

        if (IsIgnoredWorkTreePath(eventArgs.FullPath))
        {
            return;
        }

        QueueInvalidation();
    }

    private bool IsRelevantGitMetadataPath(string path)
    {
        string? gitDirectory;
        lock (_lock)
        {
            gitDirectory = _activeGitDirectory;
        }

        if (string.IsNullOrEmpty(gitDirectory))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(gitDirectory, path).Replace('\\', '/');
        return relativePath.Equals("index", StringComparison.Ordinal)
            || relativePath.Equals("index.lock", StringComparison.Ordinal)
            || relativePath.Equals("HEAD", StringComparison.Ordinal)
            || relativePath.Equals("packed-refs", StringComparison.Ordinal)
            || relativePath.StartsWith("refs/", StringComparison.Ordinal);
    }

    private bool IsCommitGraphMetadataPath(string path)
    {
        string? gitDirectory;
        lock (_lock)
        {
            gitDirectory = _activeGitDirectory;
        }

        if (string.IsNullOrEmpty(gitDirectory))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(gitDirectory, path).Replace('\\', '/');
        return relativePath.Equals("HEAD", StringComparison.Ordinal)
            || relativePath.Equals("packed-refs", StringComparison.Ordinal)
            || relativePath.StartsWith("refs/", StringComparison.Ordinal);
    }

    private static bool IsIgnoredInternalPath(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}objects{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private void OnWatcherError(object sender, ErrorEventArgs eventArgs)
    {
        QueueInvalidation();
    }

    private bool IsIgnoreRulePath(string path)
    {
        string? gitDirectory;
        lock (_lock)
        {
            gitDirectory = _activeGitDirectory;
        }

        return Path.GetFileName(path).Equals(".gitignore", StringComparison.Ordinal)
            || (!string.IsNullOrEmpty(gitDirectory)
                && string.Equals(path, Path.Combine(gitDirectory, "info", "exclude"), StringComparison.OrdinalIgnoreCase));
    }

    private bool IsIgnoredWorkTreePath(string path)
    {
        string? workTreeDirectory;
        GitIgnoreMatcher? matcher;
        lock (_lock)
        {
            workTreeDirectory = _activeWorkTreeDirectory;
            matcher = _ignoreMatcher;
        }

        if (string.IsNullOrEmpty(workTreeDirectory) || matcher == null)
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(workTreeDirectory, path).Replace('\\', '/');
        if (relativePath.StartsWith("../", StringComparison.Ordinal) || relativePath.Equals("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (relativePath.Equals(".git", StringComparison.Ordinal) || relativePath.StartsWith(".git/", StringComparison.Ordinal))
        {
            return true;
        }

        return matcher.IsIgnored(relativePath, Directory.Exists(path));
    }

    private async Task RefreshIgnoreMatcherAsync()
    {
        string? gitDirectory;
        string? workTreeDirectory;
        lock (_lock)
        {
            gitDirectory = _activeGitDirectory;
            workTreeDirectory = _activeWorkTreeDirectory;
        }

        if (string.IsNullOrEmpty(gitDirectory) || string.IsNullOrEmpty(workTreeDirectory))
        {
            return;
        }

        var matcher = await GitIgnoreMatcher
            .LoadAsync(workTreeDirectory, gitDirectory, CancellationToken.None)
            .ConfigureAwait(false);
        lock (_lock)
        {
            if (string.Equals(_activeGitDirectory, gitDirectory, StringComparison.Ordinal)
                && string.Equals(_activeWorkTreeDirectory, workTreeDirectory, StringComparison.Ordinal))
            {
                _ignoreMatcher = matcher;
            }
        }
    }

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

        await _hubContext.Clients.All
            .SendAsync("WorkingTreeChanged", notification, cancellation.Token)
            .ConfigureAwait(false);
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
        CommitGraphChangedNotification notification;
        lock (_lock)
        {
            if (_disposed || _activeRepositoryId == null || !ReferenceEquals(_graphDebounceCancellation, cancellation))
            {
                return;
            }

            notification = new CommitGraphChangedNotification
            {
                Generation = unchecked(++_graphGeneration),
            };
        }

        await _hubContext.Clients.All
            .SendAsync("CommitGraphChanged", notification, cancellation.Token)
            .ConfigureAwait(false);
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
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WorkingTreeWatcherService));
        }
    }
}
