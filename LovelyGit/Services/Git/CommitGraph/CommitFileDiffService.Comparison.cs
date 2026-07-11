using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    private static async Task<GitCommit?> ResolveComparisonCommitAsync(
        LovelyGitRepository repository,
        GitCommit commit,
        string? comparisonCommitHash,
        int parentIndex,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(comparisonCommitHash))
        {
            if (parentIndex != 0)
            {
                throw new ArgumentException(
                    "ParentIndex cannot be combined with ComparisonCommitHash.",
                    nameof(parentIndex));
            }
            if (!GitObjectId.TryParse(
                comparisonCommitHash,
                repository.ObjectFormat,
                out var comparisonId))
            {
                throw new InvalidDataException("ComparisonCommitHash is invalid.");
            }
            return await repository.GetCommitAsync(comparisonId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (commit.ParentHashes.Count > 0 && parentIndex < commit.ParentHashes.Count)
        {
            return await repository.GetCommitAsync(
                    commit.ParentHashes[parentIndex],
                    cancellationToken)
                .ConfigureAwait(false);
        }
        if (parentIndex != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parentIndex),
                "Commit parent does not exist.");
        }
        return null;
    }
}
