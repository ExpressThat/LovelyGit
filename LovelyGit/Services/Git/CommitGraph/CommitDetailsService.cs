using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitDetailsService
{
    private readonly Func<Guid, string, CancellationToken, Task<CommitDetailsResponse?>> _getCachedDetailsAsync;
    private readonly Func<Guid, string, CommitDetailsResponse, CancellationToken, Task> _saveDetailsAsync;

    public CommitDetailsService(CommitGraphRepository commitGraphRepository)
    {
        _getCachedDetailsAsync = (repositoryId, hash, cancellationToken) =>
            commitGraphRepository.GetCommitDetailsAsync(repositoryId, hash, cancellationToken);
        _saveDetailsAsync = (repositoryId, hash, details, cancellationToken) =>
            commitGraphRepository.SaveCommitDetailsAsync(
                repositoryId,
                hash,
                details,
                cancellationToken);
    }

    internal CommitDetailsService(
        Func<Guid, string, CancellationToken, Task<CommitDetailsResponse?>> getCachedDetailsAsync,
        Func<Guid, string, CommitDetailsResponse, CancellationToken, Task> saveDetailsAsync,
        bool _)
    {
        _getCachedDetailsAsync = getCachedDetailsAsync;
        _saveDetailsAsync = saveDetailsAsync;
    }

    public async Task<CommitDetailsResponse> GetCommitDetailsAsync(
        Guid repositoryId,
        string repositoryPath,
        GitObjectId commitId,
        CancellationToken cancellationToken)
    {
        return await GetCommitDetailsCoreAsync(
                repositoryId,
                repositoryPath,
                commitId,
                0,
                waitForPersistence: true,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CommitDetailsResponse> GetCommitDetailsAsync(
        Guid repositoryId,
        string repositoryPath,
        GitObjectId commitId,
        int parentIndex,
        CancellationToken cancellationToken)
    {
        return await GetCommitDetailsCoreAsync(
                repositoryId,
                repositoryPath,
                commitId,
                parentIndex,
                waitForPersistence: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<CommitDetailsResponse> GetCommitDetailsCoreAsync(
        Guid repositoryId,
        string repositoryPath,
        GitObjectId commitId,
        int parentIndex,
        bool waitForPersistence,
        CancellationToken cancellationToken)
    {
        if (parentIndex < 0) throw new ArgumentOutOfRangeException(nameof(parentIndex));
        if (parentIndex > 0)
        {
            return await BuildForParentAsync(
                    repositoryPath,
                    commitId,
                    parentIndex,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var commitHash = commitId.ToString();
        if (TryGetPendingDetails(repositoryId, commitHash, out var pendingDetails))
        {
            return pendingDetails;
        }

        var cachedDetails = await TryGetCachedCommitDetailsAsync(
                repositoryId,
                commitHash,
                cancellationToken)
            .ConfigureAwait(false);
        if (cachedDetails != null)
        {
            return cachedDetails;
        }

        var gateKey = MakeGateKey(repositoryId, commitHash);
        var gate = GetBuildGate(gateKey);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            cachedDetails = await TryGetCachedCommitDetailsAsync(
                    repositoryId,
                    commitHash,
                    cancellationToken)
                .ConfigureAwait(false);
            if (cachedDetails != null)
            {
                return cachedDetails;
            }

            if (TryGetPendingDetails(repositoryId, commitHash, out pendingDetails))
            {
                return pendingDetails;
            }

            using var repository = await LovelyGitRepository.OpenObjectDatabaseAsync(repositoryPath, cancellationToken)
                .ConfigureAwait(false);
            var commit = await repository.GetCommitAsync(commitId, cancellationToken).ConfigureAwait(false);
            var firstParent = await GetParentAsync(repository, commit, 0, cancellationToken)
                .ConfigureAwait(false);

            var details = await new CommitDetailsBuilder(repository)
                .BuildAsync(commit, firstParent, cancellationToken)
                .ConfigureAwait(false);

            await PersistOrQueueAsync(
                    repositoryId,
                    commitHash,
                    details,
                    waitForPersistence,
                    cancellationToken)
                .ConfigureAwait(false);

            return details;
        }
        finally
        {
            if (enteredGate)
            {
                gate.Semaphore.Release();
            }

            ReleaseBuildGate(gateKey, gate);
        }
    }

    private static async Task<CommitDetailsResponse> BuildForParentAsync(
        string repositoryPath,
        GitObjectId commitId,
        int parentIndex,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository
            .OpenObjectDatabaseAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var commit = await repository.GetCommitAsync(commitId, cancellationToken)
            .ConfigureAwait(false);
        var parent = await GetParentAsync(repository, commit, parentIndex, cancellationToken)
            .ConfigureAwait(false);
        return await new CommitDetailsBuilder(repository)
            .BuildAsync(commit, parent, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<GitCommit?> GetParentAsync(
        LovelyGitRepository repository,
        GitCommit commit,
        int parentIndex,
        CancellationToken cancellationToken)
    {
        if (commit.ParentHashes.Count == 0)
        {
            if (parentIndex == 0) return null;
            throw new ArgumentOutOfRangeException(nameof(parentIndex), "Commit parent does not exist.");
        }

        if (parentIndex >= commit.ParentHashes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(parentIndex), "Commit parent does not exist.");
        }

        return await repository.GetCommitAsync(commit.ParentHashes[parentIndex], cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<CommitDetailsResponse?> TryGetCachedCommitDetailsAsync(
        Guid repositoryId,
        string commitHash,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _getCachedDetailsAsync(repositoryId, commitHash, cancellationToken)
                .ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static string MakeGateKey(Guid repositoryId, string commitHash)
    {
        return string.Concat(repositoryId.ToString("N"), ':', commitHash);
    }

}
