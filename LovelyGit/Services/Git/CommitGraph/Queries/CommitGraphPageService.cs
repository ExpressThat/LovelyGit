using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;

internal sealed class CommitGraphPageService : IDisposable
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly CommitGraphRepository _commitGraphRepository;
    private readonly CommitDetailsPreloadService _commitDetailsPreloadService;
    private readonly CommitFileDiffService _commitFileDiffService;
    private readonly object _cacheWorkLock = new();
    private readonly Dictionary<Guid, CommitGraphManager> _activeGraphs = new();
    private readonly Dictionary<Guid, ActiveGraphCacheWork> _activeCacheWork = new();
    private readonly Dictionary<Guid, int> _cacheGenerations = new();
    private readonly HashSet<Guid> _repositoriesLoadedThisProcess = new();

    public CommitGraphPageService(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        CommitGraphRepository commitGraphRepository,
        CommitDetailsPreloadService commitDetailsPreloadService,
        CommitFileDiffService commitFileDiffService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _commitGraphRepository = commitGraphRepository;
        _commitDetailsPreloadService = commitDetailsPreloadService;
        _commitFileDiffService = commitFileDiffService;
    }

    public async Task<CommitGraphPageQueryResult> GetPageAsync(
        Guid knownRepositoryId,
        int limit,
        string? cursorText,
        CancellationToken cancellationToken)
    {
        var foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(knownRepositoryId)
            .ConfigureAwait(false);

        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return CommitGraphPageQueryResult.Failure("Known repository not found.");
        }

        if (limit < 0)
        {
            limit = 0;
        }

        var isFreshGraphLoad = string.IsNullOrWhiteSpace(cursorText);
        var cacheGeneration = GetCacheGeneration(foundRepo.Id);
        if (isFreshGraphLoad)
        {
            await ResetRepositoryGraphAsync(foundRepo.Id, cancellationToken).ConfigureAwait(false);
            cacheGeneration = GetCacheGeneration(foundRepo.Id);
        }

        try
        {
            var graph = await GetOrOpenGraphAsync(foundRepo.Id, foundRepo.Path, cancellationToken).ConfigureAwait(false);
            if (!graph.Success || graph.Graph == null)
            {
                return CommitGraphPageQueryResult.Failure(
                    graph.Error ?? "Failed to open native commit-graph.");
            }

            var cursorState = CommitGraphManager.DecodeCursorState(cursorText);
            var page = await graph.Graph.GetCommitGraphPageAsync(cursorState, limit, cancellationToken)
                .ConfigureAwait(false);

            var response = page.Response;
            response.NextCursor = response.HasMore ? CommitGraphManager.EncodeCursorState(page.NextCursor) : null;

            StartCacheAndPreloadDetails(foundRepo.Id, foundRepo.Path, response, cacheGeneration);

            if (!response.HasMore)
            {
                CloseGraph(foundRepo.Id);
            }

            return CommitGraphPageQueryResult.Success(response);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return CommitGraphPageQueryResult.Failure(ex.Message);
        }
    }

    public void Dispose()
    {
        List<ActiveGraphCacheWork> cacheWork;
        lock (_cacheWorkLock)
        {
            cacheWork = _activeCacheWork.Values.ToList();
            _activeCacheWork.Clear();
        }

        foreach (var work in cacheWork)
        {
            work.CancellationTokenSource.Cancel();
        }

        try
        {
            Task.WaitAll(cacheWork.Select(work => work.Task).ToArray(), TimeSpan.FromSeconds(5));
        }
        catch
        {
        }

        foreach (var work in cacheWork)
        {
            work.Dispose();
        }

        foreach (var graph in _activeGraphs.Values)
        {
            graph.Dispose();
        }

        _activeGraphs.Clear();
    }

    private async Task ResetRepositoryGraphAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        AdvanceCacheGeneration(repositoryId);
        await CancelCacheAndPreloadDetailsAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        await _commitDetailsPreloadService.CancelActiveAsync(cancellationToken).ConfigureAwait(false);
        await _commitFileDiffService.CancelRepositoryPreparationAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        if (MarkRepositoryLoaded(repositoryId))
        {
            await _commitGraphRepository.ClearRepositoryAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        }

        CloseGraph(repositoryId);
    }

    private bool MarkRepositoryLoaded(Guid repositoryId)
    {
        lock (_cacheWorkLock)
        {
            return !_repositoriesLoadedThisProcess.Add(repositoryId);
        }
    }

    private void StartCacheAndPreloadDetails(
        Guid repositoryId,
        string repositoryPath,
        Models.CommitGraphResponse response,
        int cacheGeneration)
    {
        lock (_cacheWorkLock)
        {
            if (!IsCacheGenerationCurrentCore(repositoryId, cacheGeneration))
            {
                return;
            }

            if (!_activeCacheWork.TryGetValue(repositoryId, out var work))
            {
                work = new ActiveGraphCacheWork();
                _activeCacheWork[repositoryId] = work;
            }
            else if (!work.Task.IsCompleted)
            {
                return;
            }

            work.CancellationTokenSource.Cancel();
            work.Dispose();
            work = new ActiveGraphCacheWork();
            _activeCacheWork[repositoryId] = work;
            var cancellationToken = work.CancellationTokenSource.Token;
            work.Task = Task.Run(
                () => RunCacheAndPreloadDetailsAsync(
                    work,
                    repositoryId,
                    repositoryPath,
                    response,
                    cacheGeneration,
                    cancellationToken),
                CancellationToken.None);
        }
    }

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
            await TrySaveCachedCommitsAsync(repositoryId, response, cancellationToken).ConfigureAwait(false);
            await _commitDetailsPreloadService
                .StartOrSwitchAsync(repositoryId, repositoryPath, cancellationToken)
                .ConfigureAwait(false);
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
