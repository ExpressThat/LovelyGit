using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Checkout;

internal sealed class GitCheckoutCommandService
{
    private readonly GitCliService _gitCliService;

    public GitCheckoutCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public Task CheckoutCommitDetachedAsync(
        string repositoryPath,
        string commitHash,
        CancellationToken cancellationToken)
    {
        return RunCheckoutCommandAsync(
            repositoryPath,
            ["switch", "--detach", RequireRef(commitHash, nameof(commitHash))],
            "Git checkout failed.",
            cancellationToken);
    }

    public Task CheckoutBranchAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken)
    {
        return RunCheckoutCommandAsync(
            repositoryPath,
            ["switch", GitBranchNameValidator.RequireValidBranchName(branchName, nameof(branchName))],
            "Git branch checkout failed.",
            cancellationToken);
    }

    public Task CheckoutRemoteBranchAsync(
        string repositoryPath,
        string remoteBranchName,
        string localBranchName,
        CancellationToken cancellationToken)
    {
        return RunCheckoutCommandAsync(
            repositoryPath,
            [
                "switch",
                "--track",
                "-c",
                GitBranchNameValidator.RequireValidBranchName(localBranchName, nameof(localBranchName)),
                RequireRemoteBranchName(remoteBranchName, nameof(remoteBranchName)),
            ],
            "Git remote branch checkout failed.",
            cancellationToken);
    }

    public Task CheckoutTagAsync(
        string repositoryPath,
        string tagName,
        CancellationToken cancellationToken)
    {
        return RunCheckoutCommandAsync(
            repositoryPath,
            ["switch", "--detach", RequireRef(tagName, nameof(tagName))],
            "Git tag checkout failed.",
            cancellationToken);
    }

    private async Task RunCheckoutCommandAsync(
        string repositoryPath,
        IReadOnlyList<string> arguments,
        string fallbackMessage,
        CancellationToken cancellationToken)
    {
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        var result = await _gitCliService
            .ExecuteBufferedAsync(
                arguments,
                repositoryPaths.WorkTreeDirectory,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            FirstNonEmptyLine(result.StandardError)
            ?? FirstNonEmptyLine(result.StandardOutput)
            ?? fallbackMessage);
    }

    private static string RequireRef(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Ref is required.", parameterName);
        }

        return value.Trim();
    }

    private static string RequireRemoteBranchName(string branchName, string parameterName)
    {
        if (!GitBranchNameValidator.IsValidBranchName(branchName))
        {
            throw new ArgumentException("Remote branch name is not valid.", parameterName);
        }

        return branchName.Trim();
    }

    private static string? FirstNonEmptyLine(string text)
    {
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (!trimmed.IsEmpty)
            {
                return trimmed.ToString();
            }
        }

        return null;
    }
}
