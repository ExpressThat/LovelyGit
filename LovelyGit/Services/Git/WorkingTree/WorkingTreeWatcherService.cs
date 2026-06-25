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

}
