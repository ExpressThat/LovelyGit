using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Buffers;
using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class WorkingTreeWatcherService : IDisposable
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(200);
    private const ulong FnvOffsetBasis = 14695981039346656037;
    private const ulong FnvPrime = 1099511628211;
    private readonly INativeMessaging _nativeMessaging;
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
    private ulong? _commitGraphSnapshot;
    private int _generation;
    private int _graphGeneration;
    private bool _disposed;

    public WorkingTreeWatcherService(
        INativeMessaging nativeMessaging,
        KnownGitRepositorysRepository knownGitRepositorysRepository)
    {
        _nativeMessaging = nativeMessaging;
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

        var commitGraphSnapshot = ComputeCommitGraphSnapshot(paths.GitDirectory);

        lock (_lock)
        {
            ThrowIfDisposed();
            StopActiveWatchersCore();
            _activeRepositoryId = repositoryId;
            _activeRepositoryPath = repositoryPath;
            _activeGitDirectory = paths.GitDirectory;
            _activeWorkTreeDirectory = paths.WorkTreeDirectory;
            _ignoreMatcher = null;
            _commitGraphSnapshot = commitGraphSnapshot;

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

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WorkingTreeWatcherService));
        }
    }

    private static ulong ComputeCommitGraphSnapshot(string gitDirectory)
    {
        ulong xor = 0;
        ulong sum = 0;
        ulong count = 0;
        foreach (var path in EnumerateCommitGraphSnapshotPaths(gitDirectory))
        {
            var itemHash = ComputeCommitGraphSnapshotItem(gitDirectory, path);
            xor ^= itemHash;
            sum += itemHash;
            count++;
        }

        var hash = FnvOffsetBasis;
        AddUInt64ToHash(ref hash, xor);
        AddUInt64ToHash(ref hash, sum);
        AddUInt64ToHash(ref hash, count);
        return hash;
    }

    private static IEnumerable<string> EnumerateCommitGraphSnapshotPaths(string gitDirectory)
    {
        var headPath = Path.Combine(gitDirectory, "HEAD");
        if (File.Exists(headPath))
        {
            yield return headPath;
        }

        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (File.Exists(packedRefsPath))
        {
            yield return packedRefsPath;
        }

        var refsDirectory = Path.Combine(gitDirectory, "refs");
        if (!Directory.Exists(refsDirectory))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(refsDirectory, "*", SearchOption.AllDirectories))
        {
            yield return path;
        }
    }

    private static ulong ComputeCommitGraphSnapshotItem(string gitDirectory, string path)
    {
        var hash = FnvOffsetBasis;
        AddStringToHash(ref hash, Path.GetRelativePath(gitDirectory, path).Replace('\\', '/'));
        AddByteToHash(ref hash, 0);
        AddFileToHash(ref hash, path);
        return hash;
    }

    private static void AddFileToHash(ref ulong hash, string path)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var index = 0; index < bytesRead; index++)
                {
                    AddByteToHash(ref hash, buffer[index]);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            try
            {
                var info = new FileInfo(path);
                AddUInt64ToHash(ref hash, unchecked((ulong)info.Length));
                AddUInt64ToHash(ref hash, unchecked((ulong)info.LastWriteTimeUtc.Ticks));
            }
            catch (Exception innerException) when (innerException is IOException or UnauthorizedAccessException)
            {
                AddStringToHash(ref hash, path);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void AddStringToHash(ref ulong hash, string value)
    {
        foreach (var character in value.AsSpan())
        {
            AddByteToHash(ref hash, (byte)character);
            AddByteToHash(ref hash, (byte)(character >> 8));
        }
    }

    private static void AddByteToHash(ref ulong hash, byte value)
    {
        hash ^= value;
        hash *= FnvPrime;
    }

    private static void AddUInt64ToHash(ref ulong hash, ulong value)
    {
        for (var shift = 0; shift < 64; shift += 8)
        {
            AddByteToHash(ref hash, (byte)(value >> shift));
        }
    }
}
