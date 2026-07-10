using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

namespace ExpressThat.LovelyGit.Services.Git.Rebase;

internal static class NativeInteractiveRebasePlanReader
{
    public const int MaximumCommitCount = 100;

    public static async Task<InteractiveRebasePlanResponse> ReadAsync(
        string repositoryPath,
        string baseCommitHash,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (repository.HeadTarget is not { } head)
        {
            throw new InvalidOperationException("The repository does not have a HEAD commit.");
        }

        if (string.IsNullOrWhiteSpace(repository.CurrentBranchName))
        {
            throw new InvalidOperationException("Check out a branch before starting an interactive rebase.");
        }

        if (!GitObjectId.TryParse(baseCommitHash.Trim(), repository.ObjectFormat, out var baseCommit))
        {
            throw new ArgumentException("The base commit hash is invalid.", nameof(baseCommitHash));
        }

        var commits = new List<InteractiveRebaseCommit>();
        var current = head;
        while (current != baseCommit)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (commits.Count == MaximumCommitCount)
            {
                throw new InvalidOperationException(
                    $"Interactive rebase is limited to {MaximumCommitCount} commits at a time.");
            }

            var header = await repository.GetCommitTraversalHeaderAsync(current, cancellationToken)
                .ConfigureAwait(false);
            if (header.ParentHashCount > 1)
            {
                throw new InvalidOperationException(
                    "This range contains a merge commit. Merge-preserving interactive rebase is not supported yet.");
            }

            var commit = await repository.GetCommitAsync(current, cancellationToken).ConfigureAwait(false);
            commits.Add(new InteractiveRebaseCommit
            {
                Hash = current.ToString(),
                Subject = commit.Subject,
                AuthorName = commit.AuthorName,
                AuthorUnixSeconds = commit.AuthorUnixSeconds,
            });

            if (header.ParentHashCount == 0)
            {
                throw new InvalidOperationException(
                    "The selected base is not an ancestor of the checked-out branch.");
            }

            current = header.GetParentHash(0);
        }

        if (commits.Count == 0)
        {
            throw new InvalidOperationException("Select a commit before HEAD to build a rebase plan.");
        }

        commits.Reverse();
        return new InteractiveRebasePlanResponse
        {
            BaseCommitHash = baseCommit.ToString(),
            CurrentBranchName = repository.CurrentBranchName,
            Commits = commits,
        };
    }
}
