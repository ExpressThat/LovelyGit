using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
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
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
                repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(
                paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var headState = await GitHeadReader.ReadAsync(
                paths.WorktreeGitDirectory,
                paths.GitDirectory,
                objectFormat,
                cancellationToken)
            .ConfigureAwait(false);
        if (headState.Target is not { } head)
        {
            throw new InvalidOperationException("The repository does not have a HEAD commit.");
        }

        if (string.IsNullOrWhiteSpace(headState.BranchName))
        {
            throw new InvalidOperationException("Check out a branch before starting an interactive rebase.");
        }

        if (!GitObjectId.TryParse(baseCommitHash.Trim(), objectFormat, out var baseCommit))
        {
            throw new ArgumentException("The base commit hash is invalid.", nameof(baseCommitHash));
        }

        using var objectStore = new GitObjectStore(paths.GitDirectory, objectFormat);
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

            var data = await objectStore.ReadObjectAsync(
                    current, cacheObject: false, cancellationToken)
                .ConfigureAwait(false);
            if (data.Kind != GitObjectKind.Commit)
            {
                throw new InvalidDataException($"Object is not a commit: {current}");
            }
            var commit = GitObjectParsers.ParseCommit(current, data.Data);
            if (commit.ParentHashCount > 1)
            {
                throw new InvalidOperationException(
                    "This range contains a merge commit. Merge-preserving interactive rebase is not supported yet.");
            }

            commits.Add(new InteractiveRebaseCommit
            {
                Hash = current.ToString(),
                Subject = commit.Subject,
                AuthorName = commit.AuthorName,
                AuthorUnixSeconds = commit.AuthorUnixSeconds,
            });

            if (commit.ParentHashCount == 0)
            {
                throw new InvalidOperationException(
                    "The selected base is not an ancestor of the checked-out branch.");
            }

            current = commit.GetParentHash(0);
        }

        if (commits.Count == 0)
        {
            throw new InvalidOperationException("Select a commit before HEAD to build a rebase plan.");
        }

        commits.Reverse();
        return new InteractiveRebasePlanResponse
        {
            BaseCommitHash = baseCommit.ToString(),
            CurrentBranchName = headState.BranchName,
            Commits = commits,
        };
    }
}
