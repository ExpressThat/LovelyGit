using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;

internal sealed partial class CommitGraphPageService : IDisposable
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly CommitGraphRepository _commitGraphRepository;
    private readonly CommitDetailsPreloadService _commitDetailsPreloadService;
    private readonly CommitFileDiffService _commitFileDiffService;
    private readonly CommitGraphBackgroundWorkerOptions _backgroundWorkerOptions;
    private readonly object _cacheWorkLock = new();
    private readonly Dictionary<Guid, CommitGraphManager> _activeGraphs = new();
    private readonly Dictionary<Guid, ActiveGraphCacheWork> _activeCacheWork = new();
    private readonly Dictionary<Guid, ActiveGraphCloseWork> _activeGraphCloseWork = new();
    private readonly Dictionary<Guid, int> _cacheGenerations = new();
    private readonly HashSet<Guid> _repositoriesLoadedThisProcess = new();
    private Guid? _activeRepositoryId;

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
        if (isFreshGraphLoad)
        {
            await SwitchActiveRepositoryAsync(foundRepo.Id, cancellationToken).ConfigureAwait(false);
            await ResetRepositoryGraphAsync(foundRepo.Id, cancellationToken).ConfigureAwait(false);
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

            if (!response.HasMore)
            {
                CloseGraph(foundRepo.Id);
            }
            else
            {
                ScheduleGraphClose(foundRepo.Id);
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
        List<IActiveGraphWork> cacheWork;
        List<CommitGraphManager> graphs;
        lock (_cacheWorkLock)
        {
            cacheWork = _activeCacheWork.Values.Cast<IActiveGraphWork>().ToList();
            _activeCacheWork.Clear();
            cacheWork.AddRange(_activeGraphCloseWork.Values);
            _activeGraphCloseWork.Clear();
            graphs = _activeGraphs.Values.ToList();
            _activeGraphs.Clear();
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

        foreach (var graph in graphs)
        {
            graph.Dispose();
        }
    }

    private async Task ResetRepositoryGraphAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        CancelScheduledGraphClose(repositoryId);
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

    private async Task SwitchActiveRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        List<Guid> staleRepositoryIds;
        lock (_cacheWorkLock)
        {
            if (_activeRepositoryId == repositoryId)
            {
                return;
            }

            staleRepositoryIds = _activeGraphs.Keys
                .Concat(_activeCacheWork.Keys)
                .Where(activeRepositoryId => activeRepositoryId != repositoryId)
                .Distinct()
                .ToList();
            _activeRepositoryId = repositoryId;
        }

        if (staleRepositoryIds.Count == 0)
        {
            await _commitDetailsPreloadService.CancelActiveAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        foreach (var staleRepositoryId in staleRepositoryIds)
        {
            AdvanceCacheGeneration(staleRepositoryId);
            await CancelCacheAndPreloadDetailsAsync(staleRepositoryId, cancellationToken).ConfigureAwait(false);
            await _commitFileDiffService
                .CancelRepositoryPreparationAsync(staleRepositoryId, cancellationToken)
                .ConfigureAwait(false);
            CloseGraph(staleRepositoryId);
        }

        await _commitDetailsPreloadService.CancelActiveAsync(cancellationToken).ConfigureAwait(false);
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
        if (!_backgroundWorkerOptions.EnableCommitGraphCacheWorker
            && !_backgroundWorkerOptions.EnableCommitDetailsPreloadWorker)
        {
            return;
        }

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

}
