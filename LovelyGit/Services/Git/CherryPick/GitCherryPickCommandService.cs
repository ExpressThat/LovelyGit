using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CherryPick;

internal sealed class GitCherryPickCommandService
{
    private readonly GitOperationService _operations;

    public GitCherryPickCommandService(GitCliService gitCliService)
    {
        _operations = new GitOperationService(gitCliService);
    }

    public async Task CherryPickCommitAsync(
        string repositoryPath,
        string commitHash,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        await _operations.ExecuteRequiredBufferedAsync(
            "Cherry-pick commit",
            ["cherry-pick", "--no-edit", NormalizeCommitHash(commitHash)],
            paths.WorkTreeDirectory,
            "Resolve conflicts in the working tree, then continue or abort the cherry-pick.",
            cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeCommitHash(string commitHash)
    {
        var normalized = commitHash.Trim();
        return (normalized.Length is 40 or 64) &&
               normalized.All(Uri.IsHexDigit)
            ? normalized
            : throw new ArgumentException("Commit hash is not valid.", nameof(commitHash));
    }
}
