using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.OperationState;

internal sealed class GitOperationStateService
{
    public async Task<GitOperationState> GetStateAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var gitDirectory = paths.GitDirectory;

        if (Directory.Exists(Path.Combine(gitDirectory, "rebase-merge"))
            || Directory.Exists(Path.Combine(gitDirectory, "rebase-apply")))
        {
            return Create(GitOperationKind.Rebase, "Rebase in progress");
        }

        if (File.Exists(Path.Combine(gitDirectory, "MERGE_HEAD")))
        {
            return Create(GitOperationKind.Merge, "Merge in progress");
        }

        if (File.Exists(Path.Combine(gitDirectory, "CHERRY_PICK_HEAD")))
        {
            return Create(GitOperationKind.CherryPick, "Cherry-pick in progress");
        }

        if (File.Exists(Path.Combine(gitDirectory, "REVERT_HEAD")))
        {
            return Create(GitOperationKind.Revert, "Revert in progress");
        }

        if (File.Exists(Path.Combine(gitDirectory, "BISECT_LOG")))
        {
            return Create(GitOperationKind.Bisect, "Bisect in progress");
        }

        return Create(GitOperationKind.None, "Ready");
    }

    private static GitOperationState Create(
        GitOperationKind kind,
        string label) =>
        new()
        {
            Kind = kind,
            Label = label,
            Description = kind == GitOperationKind.None
                ? "No Git operation is in progress."
                : "Resolve or continue the operation from Git before starting another history-changing action.",
        };
}
