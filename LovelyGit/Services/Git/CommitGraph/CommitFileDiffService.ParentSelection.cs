using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    public async Task<CommitFileDiffResponse> GetCommitFileDiffAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        int parentIndex,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        if (parentIndex < 0) throw new ArgumentOutOfRangeException(nameof(parentIndex));
        if (parentIndex > 0 || ignoreWhitespace)
        {
            return await BuildCommitFileDiffAsync(
                    repositoryPath,
                    commitHash,
                    null,
                    parentIndex,
                    path,
                    viewMode,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var cached = await TryGetCachedDiffAsync(
                repositoryId,
                commitHash,
                path,
                viewMode,
                ignoreWhitespace,
                cancellationToken)
            .ConfigureAwait(false);
        if (cached != null) return cached;

        return await BuildAndCacheMissingDiffAsync(
                repositoryId,
                repositoryPath,
                commitHash,
                path,
                viewMode,
                ignoreWhitespace,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<CommitFileDiffResponse> GetCommitComparisonFileDiffAsync(
        string repositoryPath,
        string commitHash,
        string comparisonCommitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken) =>
        BuildCommitFileDiffAsync(
            repositoryPath,
            commitHash,
            comparisonCommitHash,
            0,
            path,
            viewMode,
            ignoreWhitespace,
            cancellationToken);
}
