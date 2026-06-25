using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;

internal sealed partial class CommitGraphPageService : IDisposable
{
    private async Task RunCacheAndPreloadDetailsAsync(
        ActiveGraphCacheWork work,
        Guid repositoryId,
        string repositoryPath,
        Models.CommitGraphResponse response,
        int cacheGeneration,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCacheGenerationCurrent(repositoryId, cacheGeneration))
            {
                return;
            }

            await CacheAndPreloadDetailsAsync(repositoryId, repositoryPath, response, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_cacheWorkLock)
            {
                if (_activeCacheWork.TryGetValue(repositoryId, out var activeWork)
                    && ReferenceEquals(activeWork, work))
                {
                    _activeCacheWork.Remove(repositoryId);
                    work.Dispose();
                }
            }
        }
    }

    private async Task CancelCacheAndPreloadDetailsAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        ActiveGraphCacheWork? work;
        lock (_cacheWorkLock)
        {
            if (!_activeCacheWork.Remove(repositoryId, out work))
            {
                return;
            }

            work.CancellationTokenSource.Cancel();
        }

        try
        {
            await work.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            work.Dispose();
        }
    }

    private int GetCacheGeneration(Guid repositoryId)
    {
        lock (_cacheWorkLock)
        {
            _cacheGenerations.TryGetValue(repositoryId, out var generation);
            return generation;
        }
    }

    private void AdvanceCacheGeneration(Guid repositoryId)
    {
        lock (_cacheWorkLock)
        {
            _cacheGenerations.TryGetValue(repositoryId, out var generation);
            _cacheGenerations[repositoryId] = unchecked(generation + 1);
        }
    }

    private bool IsCacheGenerationCurrent(Guid repositoryId, int generation)
    {
        lock (_cacheWorkLock)
        {
            return IsCacheGenerationCurrentCore(repositoryId, generation);
        }
    }

    private bool IsCacheGenerationCurrentCore(Guid repositoryId, int generation)
    {
        _cacheGenerations.TryGetValue(repositoryId, out var currentGeneration);
        return currentGeneration == generation;
    }

    private async Task CacheAndPreloadDetailsAsync(
        Guid repositoryId,
        string repositoryPath,
        Models.CommitGraphResponse response,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_backgroundWorkerOptions.EnableCommitGraphCacheWorker)
            {
                await TrySaveCachedCommitsAsync(repositoryId, response, cancellationToken).ConfigureAwait(false);
            }

            if (_backgroundWorkerOptions.EnableCommitDetailsPreloadWorker)
            {
                await _commitDetailsPreloadService
                    .StartOrSwitchAsync(repositoryId, repositoryPath, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch
        {
        }
    }

    private async Task<CommitGraphOpenResult> GetOrOpenGraphAsync(
        Guid repositoryId,
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        if (_activeGraphs.TryGetValue(repositoryId, out var graph))
        {
            return new CommitGraphOpenResult(true, graph, null);
        }

        var openResult = await CommitGraphManager.TryOpenAsync(
                repositoryPath,
                repositoryId,
                _commitGraphRepository,
                cancellationToken)
            .ConfigureAwait(false);
        if (openResult.Success && openResult.Graph != null)
        {
            _activeGraphs[repositoryId] = openResult.Graph;
        }

        return openResult;
    }

    private async Task TrySaveCachedCommitsAsync(
        Guid repositoryId,
        Models.CommitGraphResponse response,
        CancellationToken cancellationToken)
    {
        try
        {
            await _commitGraphRepository
                .SaveCachedCommitsAsync(repositoryId, response, cancellationToken)
                .ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void CloseGraph(Guid repositoryId)
    {
        if (_activeGraphs.Remove(repositoryId, out var completedGraph))
        {
            completedGraph.Dispose();
        }
    }

    private sealed class ActiveGraphCacheWork : IDisposable
    {
        public CancellationTokenSource CancellationTokenSource { get; } = new();

        public Task Task { get; set; } = Task.CompletedTask;

        public void Dispose()
        {
            CancellationTokenSource.Dispose();
        }
    }
}
