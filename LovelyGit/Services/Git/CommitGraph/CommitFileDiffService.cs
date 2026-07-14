using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService : IDisposable
{
    private const int MaxSyntaxHighlightedCharacters = 750_000;
    private const int MaxSyntaxHighlightedLineLength = 2_000;
    private readonly ICommitFileDiffCache? _persistentCache;
    private readonly object _preparationLock = new();
    private readonly object _diffBuildGateLock = new();
    private readonly Dictionary<Guid, ActivePreparation> _activePreparations = new();
    private readonly Dictionary<string, BuildGate> _diffBuildGates = new(StringComparer.Ordinal);
    private readonly CommitFileDiffSourceCache _sourceCache = new();
    private bool _disposed;

    public CommitFileDiffService(CommitGraphRepository commitGraphRepository)
    {
        _persistentCache = commitGraphRepository is null
            ? null
            : new CommitFileDiffCacheAdapter(commitGraphRepository);
    }

    internal CommitFileDiffService(ICommitFileDiffCache persistentCache, bool _)
    {
        _persistentCache = persistentCache;
    }

    public void StartPreparingCommitDiffs(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        IReadOnlyList<CommitChangedFile> changedFiles)
    {
        ActivePreparation? previous = null;
        var shouldStart = true;
        lock (_preparationLock)
        {
            ThrowIfDisposed();
            if (_activePreparations.TryGetValue(repositoryId, out previous))
            {
                if (string.Equals(previous.CommitHash, commitHash, StringComparison.Ordinal))
                {
                    shouldStart = false;
                }
                else
                {
                    previous.CancellationTokenSource.Cancel();
                }
            }

            if (shouldStart)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var previousPreparation = previous;
                var task = Task.Run(
                    async () =>
                    {
                        if (previousPreparation != null)
                        {
                            try
                            {
                                await previousPreparation.Task.ConfigureAwait(false);
                            }
                            finally
                            {
                                previousPreparation.Dispose();
                            }
                        }

                        await PrepareCommitDiffsAsync(
                                repositoryId,
                                repositoryPath,
                                commitHash,
                                changedFiles,
                                previousPreparation?.CommitHash,
                                cancellationTokenSource.Token)
                            .ConfigureAwait(false);
                    },
                    cancellationTokenSource.Token);

                _activePreparations[repositoryId] = new ActivePreparation(
                    commitHash,
                    cancellationTokenSource,
                    task);
            }
        }

        if (!shouldStart)
        {
            return;
        }
    }

    public async Task<CommitFileDiffResponse> GetCommitFileDiffAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        return await GetCommitFileDiffAsync(
                repositoryId,
                repositoryPath,
                commitHash,
                0,
                path,
                viewMode,
                ignoreWhitespace,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public void CancelPreparingCommitDiffs(Guid repositoryId, string commitHash)
    {
        ActivePreparation? active = null;
        lock (_preparationLock)
        {
            if (_activePreparations.TryGetValue(repositoryId, out active)
                && string.Equals(active.CommitHash, commitHash, StringComparison.Ordinal))
            {
                _activePreparations.Remove(repositoryId);
                active.CancellationTokenSource.Cancel();
            }
            else
            {
                active = null;
            }
        }

        if (active != null)
        {
            _ = active.Task.ContinueWith(
                static (task, state) => ((ActivePreparation)state!).Dispose(),
                active,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    public async Task CancelRepositoryPreparationAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        ActivePreparation? active = null;
        lock (_preparationLock)
        {
            if (_activePreparations.Remove(repositoryId, out active))
            {
                active.CancellationTokenSource.Cancel();
            }
        }

        if (active == null)
        {
            return;
        }

        try
        {
            await active.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            active.Dispose();
        }
    }

    public void Dispose()
    {
        StopAndWait();
    }

    public void StopAndWait()
    {
        List<ActivePreparation> active;
        lock (_preparationLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            active = _activePreparations.Values.ToList();
            _activePreparations.Clear();
        }

        foreach (var preparation in active)
        {
            preparation.CancellationTokenSource.Cancel();
        }

        try
        {
            Task.WaitAll(active.Select(preparation => preparation.Task).ToArray(), TimeSpan.FromSeconds(5));
        }
        catch
        {
        }

        foreach (var preparation in active)
        {
            preparation.Dispose();
        }
        _sourceCache.Clear();
    }

}
