using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.Tags;

namespace ExpressThat.LovelyGit.Services.Git.Checkout;

internal sealed class GitCheckoutCommandService
{
    private readonly GitOperationService _operations;

    public GitCheckoutCommandService(GitCliService gitCliService)
    {
        _operations = new GitOperationService(gitCliService);
    }

    public Task CheckoutCommitDetachedAsync(
        string repositoryPath,
        string commitHash,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Check out commit",
            ["switch", "--quiet", "--detach", NormalizeCommitHash(commitHash)],
            cancellationToken);

    public Task CheckoutBranchAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Check out branch",
            ["switch", "--quiet", "--", NormalizeBranchName(branchName)],
            cancellationToken);

    public Task CheckoutTagAsync(
        string repositoryPath,
        string tagName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Check out tag",
            ["switch", "--quiet", "--detach", NormalizeTagName(tagName)],
            cancellationToken);

    public Task CheckoutRemoteBranchAsync(
        string repositoryPath,
        string remoteBranchName,
        string localBranchName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Check out remote branch",
            [
                "switch",
                "--quiet",
                "--create",
                NormalizeBranchName(localBranchName),
                "--track",
                NormalizeRemoteBranchName(remoteBranchName),
            ],
            cancellationToken);

    private async Task RunAsync(
        string repositoryPath,
        string operationName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        await _operations.ExecuteRequiredBufferedAsync(
            operationName,
            arguments,
            paths.WorkTreeDirectory,
            "Commit, stash, or discard conflicting working changes, then try again.",
            cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeBranchName(string branchName)
    {
        var normalized = branchName.Trim();
        return GitBranchNameValidator.IsValidBranchName(normalized)
            ? normalized
            : throw new ArgumentException("Branch name is not valid.", nameof(branchName));
    }

    private static string NormalizeTagName(string tagName)
    {
        var normalized = tagName.Trim();
        return GitTagNameValidator.IsValidTagName(normalized)
            ? normalized
            : throw new ArgumentException("Tag name is not valid.", nameof(tagName));
    }

    private static string NormalizeCommitHash(string commitHash)
    {
        var normalized = commitHash.Trim();
        if ((normalized.Length is not 40 and not 64) ||
            normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Commit hash is not valid.", nameof(commitHash));
        }

        return normalized;
    }

    private static string NormalizeRemoteBranchName(string branchName)
    {
        var normalized = branchName.Trim();
        var separator = normalized.IndexOf('/');
        if (separator <= 0 || separator == normalized.Length - 1 ||
            !GitRemoteNameValidator.IsValidRemoteName(normalized[..separator]) ||
            !GitBranchNameValidator.IsValidBranchName(normalized[(separator + 1)..]))
        {
            throw new ArgumentException("Remote branch name is not valid.", nameof(branchName));
        }

        return normalized;
    }
}
