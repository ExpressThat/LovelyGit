using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed class GitBranchCommandService
{
    private readonly GitOperationService _gitOperationService;

    public GitBranchCommandService(GitOperationService gitOperationService)
    {
        _gitOperationService = gitOperationService;
    }

    public Task CheckoutAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Switch branch",
            ["switch", "--quiet", "--", NormalizeBranchName(branchName)],
            "Commit, stash, or discard conflicting working changes, then try again.",
            cancellationToken);

    public Task CreateAsync(
        string repositoryPath,
        string branchName,
        string? startPoint,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "switch",
            "--quiet",
            "--create",
            NormalizeBranchName(branchName),
        };
        if (!string.IsNullOrWhiteSpace(startPoint))
        {
            arguments.Add(startPoint.Trim());
        }

        return RunAsync(
            repositoryPath,
            "Create branch",
            arguments,
            "Choose a unique valid branch name and verify the start point exists.",
            cancellationToken);
    }

    private async Task RunAsync(
        string repositoryPath,
        string operationName,
        IReadOnlyList<string> arguments,
        string recoveryHint,
        CancellationToken cancellationToken)
    {
        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        await _gitOperationService.ExecuteRequiredBufferedAsync(
            operationName,
            arguments,
            repositoryPaths.WorkTreeDirectory,
            recoveryHint,
            cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeBranchName(string branchName)
    {
        var normalized = branchName.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Branch name is required.", nameof(branchName));
        }

        if (normalized.Length > 255 || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("Branch name is not valid.", nameof(branchName));
        }

        return normalized;
    }
}
