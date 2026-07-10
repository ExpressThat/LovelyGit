using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Revert;

internal sealed class GitRevertCommandService
{
    private readonly GitOperationService _operations;

    public GitRevertCommandService(GitCliService gitCliService)
    {
        _operations = new GitOperationService(gitCliService);
    }

    public async Task RevertCommitAsync(
        string repositoryPath,
        string commitHash,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        await _operations.ExecuteRequiredBufferedAsync(
            "Revert commit",
            ["revert", "--no-edit", NormalizeCommitHash(commitHash)],
            paths.WorkTreeDirectory,
            "Resolve conflicts in the working tree, then continue or abort the revert.",
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
