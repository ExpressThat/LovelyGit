using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    private async Task<CommitFileDiffResponse?> TryGetCachedDiffAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        try
        {
            var cached = await _commitGraphRepository
                .GetCommitFileDiffAsync(
                    repositoryId,
                    commitHash,
                    path,
                    viewMode,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
            if (cached is not null && !IsValidCachedDiff(cached))
                throw new InvalidDataException("Cached diff payload is incomplete.");
            if (cached is not null)
            {
                var expandedLineCount = cached.Lines.Count;
                CompactDiffPayloadBuilder.CompactIfUseful(cached);
                if (!CommitFileDiffCachingPolicy.ShouldPersist(cached))
                {
                    await _commitGraphRepository.RemoveCommitFileDiffAsync(
                            repositoryId,
                            commitHash,
                            path,
                            viewMode,
                            ignoreWhitespace,
                            CancellationToken.None)
                        .ConfigureAwait(false);
                    return null;
                }
                if (expandedLineCount > 0 && cached.Lines.Count == 0)
                {
                    _ = PersistCompactedCachedDiffAsync(
                        repositoryId,
                        commitHash,
                        path,
                        ignoreWhitespace,
                        cached);
                }
            }
            return cached;
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _commitGraphRepository
                    .ClearCommitFileDiffsAsync(repositoryId, commitHash, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch
            {
            }

            return null;
        }
    }

    private async Task PersistCompactedCachedDiffAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        bool ignoreWhitespace,
        CommitFileDiffResponse response)
    {
        if (!CommitFileDiffCachingPolicy.ShouldPersist(response)) return;
        try
        {
            await _commitGraphRepository.SaveCommitFileDiffAsync(
                    repositoryId,
                    commitHash,
                    path,
                    response,
                    ignoreWhitespace,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // Cache migration must never fail the user's diff request.
        }
    }
}
