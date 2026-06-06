using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitDetailsService
{
    private readonly CommitGraphRepository _commitGraphRepository;

    public CommitDetailsService(CommitGraphRepository commitGraphRepository)
    {
        _commitGraphRepository = commitGraphRepository;
    }

    public async Task<CommitDetailsResponse> GetCommitDetailsAsync(
        Guid repositoryId,
        string repositoryPath,
        GitObjectId commitId,
        CancellationToken cancellationToken)
    {
        var commitHash = commitId.ToString();
        var cachedDetails = await TryGetCachedCommitDetailsAsync(
                repositoryId,
                commitHash,
                cancellationToken)
            .ConfigureAwait(false);
        if (cachedDetails != null)
        {
            return cachedDetails;
        }

        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var commit = await repository.GetCommitAsync(commitId, cancellationToken).ConfigureAwait(false);
        GitCommit? firstParent = null;
        if (commit.ParentHashes.Count > 0)
        {
            firstParent = await repository.GetCommitAsync(commit.ParentHashes[0], cancellationToken)
                .ConfigureAwait(false);
        }

        var details = await new CommitDetailsBuilder(repository)
            .BuildAsync(commit, firstParent, cancellationToken)
            .ConfigureAwait(false);

        await TrySaveCommitDetailsAsync(repositoryId, commitHash, details, cancellationToken)
            .ConfigureAwait(false);

        return details;
    }

    private async Task<CommitDetailsResponse?> TryGetCachedCommitDetailsAsync(
        Guid repositoryId,
        string commitHash,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _commitGraphRepository
                .GetCommitDetailsAsync(repositoryId, commitHash, cancellationToken)
                .ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task TrySaveCommitDetailsAsync(
        Guid repositoryId,
        string commitHash,
        CommitDetailsResponse details,
        CancellationToken cancellationToken)
    {
        try
        {
            await _commitGraphRepository
                .SaveCommitDetailsAsync(repositoryId, commitHash, details, cancellationToken)
                .ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
        }
    }
}
