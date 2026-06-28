using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class RepositoryRefsService
{
    private const int InitialTagLimit = 500;
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

        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(knownRepository.Path, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery
            .ReadObjectFormatAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var summary = await GitRefSummaryReader
            .ReadAsync(paths.GitDirectory, objectFormat, InitialTagLimit, cancellationToken)
            .ConfigureAwait(false);
        var remoteUrl = await GitRemoteConfigReader
            .ReadPrimaryRemoteUrlAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);

        return new RepositoryRefsResponse
        {
            CurrentBranchName = summary.CurrentBranchName,
            RemotePrefixes = summary.RemotePrefixes.ToList(),
            Refs = summary.Refs.Select(reference => ToItem(reference, remoteUrl)).ToList(),
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
}
