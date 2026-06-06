using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;

internal sealed class CommitGraphPageService : IDisposable
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly CommitGraphRepository _commitGraphRepository;
    private readonly CommitDetailsPreloadService _commitDetailsPreloadService;
    private readonly CommitFileDiffService _commitFileDiffService;
    private readonly Dictionary<Guid, CommitGraphManager> _activeGraphs = new();

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
        if (isFreshGraphLoad)
        {
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

            _ = CacheAndPreloadDetailsAsync(foundRepo.Id, foundRepo.Path, response, CancellationToken.None);

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
        foreach (var graph in _activeGraphs.Values)
        {
            graph.Dispose();
        }

        _activeGraphs.Clear();
    }

    private async Task ResetRepositoryGraphAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        await _commitDetailsPreloadService.CancelActiveAsync(cancellationToken).ConfigureAwait(false);
        await _commitFileDiffService.CancelRepositoryPreparationAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        await _commitGraphRepository.ClearRepositoryAsync(repositoryId).ConfigureAwait(false);
        CloseGraph(repositoryId);
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
}
