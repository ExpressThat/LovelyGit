using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Worktrees;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class RepositoryRefsService
{
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public RepositoryRefsService(KnownGitRepositorysRepository knownRepositories)
    {
        _knownRepositories = knownRepositories;
    }

    public async Task<RepositoryRefsResponse?> GetRefsAsync(
        Guid repositoryId,
        CancellationToken cancellationToken)
    {
        var knownRepository = await _knownRepositories.FindByIdAsync(repositoryId)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(knownRepository?.Path))
        {
            return null;
        }

        return await ReadAsync(knownRepository.Path, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<RepositoryRefsResponse> ReadAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery
            .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var summaryTask = GitRefSummaryReader.ReadAsync(
            paths.GitDirectory, objectFormat, GitRefReader.DefaultTagLimit, cancellationToken);
        var remoteUrlTask = GitRemoteConfigReader.ReadPrimaryRemoteUrlAsync(
            paths.GitDirectory, cancellationToken);
        var worktreesTask = GitWorktreeReader.ReadAsync(
            paths.WorktreeGitDirectory, paths.WorkTreeDirectory, cancellationToken);
        var stashesTask = GitStashReader.ReadAsync(
            paths.GitDirectory, objectFormat, cancellationToken);
        var upstreamsTask = GitBranchUpstreamConfigReader.ReadAsync(
            paths.GitDirectory, cancellationToken);
        await Task.WhenAll(
                summaryTask,
                remoteUrlTask,
                worktreesTask,
                stashesTask,
                upstreamsTask)
            .ConfigureAwait(false);
        var summary = await summaryTask.ConfigureAwait(false);
        var remoteUrl = await remoteUrlTask.ConfigureAwait(false);
        var worktrees = await worktreesTask.ConfigureAwait(false);
        var stashes = await stashesTask.ConfigureAwait(false);
        var upstreams = await upstreamsTask.ConfigureAwait(false);
        var currentBranchName = worktrees.First(worktree => worktree.IsCurrent).BranchName;

        return new RepositoryRefsResponse
        {
            CurrentBranchName = currentBranchName,
            RemotePrefixes = summary.RemotePrefixes.ToList(),
            Refs = summary.Refs.Select(reference => ToItem(reference, remoteUrl)).ToList(),
            Worktrees = worktrees.Select(ToItem).ToList(),
            Stashes = stashes.Select(ToItem).ToList(),
            BranchUpstreams = upstreams
                .Select(upstream => new RepositoryBranchUpstreamItem
                {
                    BranchName = upstream.BranchName,
                    UpstreamName = upstream.UpstreamName,
                })
                .ToList(),
        };
    }

    private static RepositoryRefItem ToItem(GitRef reference, string? remoteUrl) =>
        new()
        {
            Name = reference.Name,
            CommitHash = reference.Target.ToString(),
            Kind = ToKind(reference.Kind),
            RemoteUrl = reference.Kind == GitRefKind.Tag
                ? RemoteCommitUrlBuilder.BuildTag(remoteUrl, reference.Name)
                : null,
        };

    private static CommitRefKind ToKind(GitRefKind kind) =>
        kind switch
        {
            GitRefKind.Remote => CommitRefKind.Remote,
            GitRefKind.Tag => CommitRefKind.Tag,
            GitRefKind.Stash => CommitRefKind.Stash,
            _ => CommitRefKind.Local,
        };

    private static RepositoryWorktreeItem ToItem(GitWorktree worktree) =>
        new()
        {
            Path = worktree.Path,
            BranchName = worktree.BranchName,
            IsCurrent = worktree.IsCurrent,
            IsLocked = worktree.IsLocked,
            LockReason = worktree.LockReason,
        };

    private static RepositoryStashItem ToItem(GitStashEntry stash) =>
        new()
        {
            Selector = stash.Selector,
            CommitHash = stash.Target.ToString(),
            Message = stash.Message,
            CreatedAtUnixSeconds = stash.CreatedAtUnixSeconds,
        };
}
