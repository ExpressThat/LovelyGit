using ExpressThat.LovelyGit.Services.Data.Repositorys;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;

internal sealed partial class CommitGraphPageService
{
    private static readonly TimeSpan DefaultActiveGraphIdleCloseDelay = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _activeGraphIdleCloseDelay;

    public CommitGraphPageService(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        CommitGraphRepository commitGraphRepository,
        CommitDetailsPreloadService commitDetailsPreloadService,
        CommitFileDiffService commitFileDiffService,
        CommitGraphBackgroundWorkerOptions backgroundWorkerOptions)
        : this(
            knownGitRepositorysRepository,
            commitGraphRepository,
            commitDetailsPreloadService,
            commitFileDiffService,
            backgroundWorkerOptions,
            DefaultActiveGraphIdleCloseDelay)
    {
    }

    internal CommitGraphPageService(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        CommitGraphRepository commitGraphRepository,
        CommitDetailsPreloadService commitDetailsPreloadService,
        CommitFileDiffService commitFileDiffService,
        CommitGraphBackgroundWorkerOptions backgroundWorkerOptions,
        TimeSpan activeGraphIdleCloseDelay)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _commitGraphRepository = commitGraphRepository;
        _commitDetailsPreloadService = commitDetailsPreloadService;
        _commitFileDiffService = commitFileDiffService;
        _backgroundWorkerOptions = backgroundWorkerOptions;
        _activeGraphIdleCloseDelay = activeGraphIdleCloseDelay;
    }
}
