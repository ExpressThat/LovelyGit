using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitDetailsService
{
    private readonly CommitGraphRepository _commitGraphRepository;
    private readonly object _commitBuildGateLock = new();
    private readonly Dictionary<string, BuildGate> _commitBuildGates = new(StringComparer.Ordinal);

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
        return await GetCommitDetailsAsync(
                repositoryId,
                repositoryPath,
                commitId,
                0,
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

            using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
                .ConfigureAwait(false);
            var commit = await repository.GetCommitAsync(commitId, cancellationToken).ConfigureAwait(false);
            var firstParent = await GetParentAsync(repository, commit, 0, cancellationToken)
                .ConfigureAwait(false);

            var details = await new CommitDetailsBuilder(repository)
                .BuildAsync(commit, firstParent, cancellationToken)
                .ConfigureAwait(false);

            await _commitGraphRepository
                .SaveCommitDetailsAsync(repositoryId, commitHash, details, cancellationToken)
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
            .OpenAsync(repositoryPath, cancellationToken)
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
            return await _commitGraphRepository
                .GetCommitDetailsAsync(repositoryId, commitHash, cancellationToken)
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

    private BuildGate GetBuildGate(string key)
    {
        lock (_commitBuildGateLock)
        {
            if (!_commitBuildGates.TryGetValue(key, out var gate))
            {
                gate = new BuildGate();
                _commitBuildGates[key] = gate;
            }

            gate.ReferenceCount++;
            return gate;
        }
    }

    private void ReleaseBuildGate(string key, BuildGate gate)
    {
        lock (_commitBuildGateLock)
        {
            gate.ReferenceCount--;
            if (gate.ReferenceCount == 0
                && _commitBuildGates.TryGetValue(key, out var activeGate)
                && ReferenceEquals(activeGate, gate))
            {
                _commitBuildGates.Remove(key);
                gate.Semaphore.Dispose();
            }
        }
    }

    private sealed class BuildGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }
    }
}
