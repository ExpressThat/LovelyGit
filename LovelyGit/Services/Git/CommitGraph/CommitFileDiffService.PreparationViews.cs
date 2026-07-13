using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    private async Task SaveMissingViewModesAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CancellationToken cancellationToken)
    {
        var hasSideBySide = await HasCachedViewAsync(
            repositoryId, commitHash, path, CommitDiffViewMode.SideBySide, cancellationToken);
        var hasCombined = await HasCachedViewAsync(
            repositoryId, commitHash, path, CommitDiffViewMode.Combined, cancellationToken);
        if (hasSideBySide && hasCombined) return;

        var source = await BuildCommitFileDiffSourceAsync(
                repositoryPath, commitHash, null, 0, path, cancellationToken)
            .ConfigureAwait(false);
        if (!source.IsBinary
            && !CommitFileDiffPreparationPolicy.CanPersistPreparedText(source.OldText, source.NewText))
        {
            return;
        }

        if (!hasSideBySide && !hasCombined)
        {
            await BuildAndCacheMissingDiffPairAsync(
                    repositoryId,
                    commitHash,
                    path,
                    source,
                    cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (!hasSideBySide)
        {
            await BuildAndCacheMissingDiffAsync(
                    repositoryId,
                    repositoryPath,
                    commitHash,
                    path,
                    CommitDiffViewMode.SideBySide,
                    source,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (!hasCombined)
        {
            await BuildAndCacheMissingDiffAsync(
                    repositoryId,
                    repositoryPath,
                    commitHash,
                    path,
                    CommitDiffViewMode.Combined,
                    source,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private Task<bool> HasCachedViewAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken) =>
        _commitGraphRepository.HasCommitFileDiffAsync(
            repositoryId,
            commitHash,
            path,
            viewMode,
            ignoreWhitespace: false,
            cancellationToken);
}
