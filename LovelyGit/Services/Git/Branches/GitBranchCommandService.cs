using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Branches;

internal sealed class GitBranchCommandService
{
    private readonly GitCliService _gitCliService;

    public GitBranchCommandService(GitCliService gitCliService)
    {
        _gitCliService = gitCliService;
    }

    public Task CreateBranchAsync(
        string repositoryPath,
        string branchName,
        string startPoint,
        CancellationToken cancellationToken)
    {
        return RunBranchCommandAsync(
            repositoryPath,
            ["branch", GitBranchNameValidator.RequireValidBranchName(branchName, nameof(branchName)), RequireStartPoint(startPoint)],
            "Git branch creation failed.",
            cancellationToken);
    }

    public async Task<string?> GetCurrentBranchNameAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        var result = await _gitCliService
            .ExecuteBufferedAsync(
                ["branch", "--show-current"],
                repositoryPaths.WorkTreeDirectory,
                validateExitCode: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            return null;
        }

        var branchName = result.StandardOutput.Trim();
        return branchName.Length == 0 ? null : branchName;
    }

    public Task CreateBranchFromTagAsync(
        string repositoryPath,
        string branchName,
        string tagName,
        CancellationToken cancellationToken)
    {
        return CreateBranchAsync(repositoryPath, branchName, tagName, cancellationToken);
    }

    public Task RenameBranchAsync(
        string repositoryPath,
        string branchName,
        string newBranchName,
        CancellationToken cancellationToken)
    {
        return RunBranchCommandAsync(
            repositoryPath,
            [
                "branch",
                "-m",
                GitBranchNameValidator.RequireValidBranchName(branchName, nameof(branchName)),
                GitBranchNameValidator.RequireValidBranchName(newBranchName, nameof(newBranchName)),
            ],
            "Git branch rename failed.",
            cancellationToken);
    }

    public Task DeleteBranchAsync(
        string repositoryPath,
        string branchName,
        bool force,
        CancellationToken cancellationToken)
    {
        return RunBranchCommandAsync(
            repositoryPath,
            ["branch", force ? "-D" : "-d", GitBranchNameValidator.RequireValidBranchName(branchName, nameof(branchName))],
            "Git branch delete failed.",
            cancellationToken);
    }

    public Task PushBranchAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken)
    {
        return RunBranchCommandAsync(
            repositoryPath,
            ["push", "-u", "origin", GitBranchNameValidator.RequireValidBranchName(branchName, nameof(branchName))],
            "Git branch push failed.",
            cancellationToken);
    }

    public Task PullBranchAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken)
    {
        return RunBranchCommandAsync(
            repositoryPath,
            ["pull", "--ff-only", "origin", GitBranchNameValidator.RequireValidBranchName(branchName, nameof(branchName))],
            "Git branch pull failed.",
            cancellationToken);
    }

    public Task SetBranchUpstreamAsync(
        string repositoryPath,
        string branchName,
        string upstreamName,
        CancellationToken cancellationToken)
    {
        return RunBranchCommandAsync(
            repositoryPath,
            [
                "branch",
                "--set-upstream-to",
                RequireRemoteBranchName(upstreamName, nameof(upstreamName)),
                GitBranchNameValidator.RequireValidBranchName(branchName, nameof(branchName)),
            ],
            "Git branch upstream update failed.",
            cancellationToken);
    }

    public Task UnsetBranchUpstreamAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken)
    {
        return RunBranchCommandAsync(
            repositoryPath,
            ["branch", "--unset-upstream", GitBranchNameValidator.RequireValidBranchName(branchName, nameof(branchName))],
            "Git branch upstream update failed.",
            cancellationToken);
    }

    private async Task RunBranchCommandAsync(
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

    private static string RequireStartPoint(string startPoint)
    {
        if (string.IsNullOrWhiteSpace(startPoint))
        {
            throw new ArgumentException("Start point is required.", nameof(startPoint));
        }

        return startPoint.Trim();
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
