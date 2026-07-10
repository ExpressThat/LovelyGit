using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.Tags;

namespace ExpressThat.LovelyGit.Services.Git.Branches;

internal sealed partial class GitBranchCommandService
{
    private readonly GitOperationService _operations;

    public GitBranchCommandService(GitCliService gitCliService)
    {
        _operations = new GitOperationService(gitCliService);
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

    public Task CreateBranchAsync(
        string repositoryPath,
        string branchName,
        string commitHash,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Create branch",
            ["branch", "--", NormalizeBranchName(branchName), NormalizeCommitHash(commitHash)],
            "Choose a unique valid branch name and verify the target commit exists.",
            cancellationToken);

    public Task CreateBranchFromTagAsync(
        string repositoryPath,
        string branchName,
        string tagName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Create branch from tag",
            ["branch", "--", NormalizeBranchName(branchName), NormalizeTagName(tagName)],
            "Choose a unique branch name and verify the tag still exists.",
            cancellationToken);

    public Task RenameBranchAsync(
        string repositoryPath,
        string branchName,
        string newBranchName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Rename branch",
            ["branch", "--move", NormalizeBranchName(branchName), NormalizeBranchName(newBranchName)],
            "Choose a unique valid branch name and try again.",
            cancellationToken);

    public Task DeleteBranchAsync(
        string repositoryPath,
        string branchName,
        bool force,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            "Delete branch",
            ["branch", force ? "-D" : "-d", "--", NormalizeBranchName(branchName)],
            force
                ? "Switch away from this branch before deleting it."
                : "Only merged branches can be safely deleted. Enable force delete only if its commits are no longer needed.",
            cancellationToken);

    private async Task RunAsync(
        string repositoryPath,
        string operationName,
        IReadOnlyList<string> arguments,
        string recoveryHint,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        await _operations.ExecuteRequiredBufferedAsync(
            operationName,
            arguments,
            paths.WorkTreeDirectory,
            recoveryHint,
            cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeBranchName(string branchName)
    {
        var normalized = branchName.Trim();
        return GitBranchNameValidator.IsValidBranchName(normalized)
            ? normalized
            : throw new ArgumentException("Branch name is not valid.", nameof(branchName));
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

    private static string NormalizeTagName(string tagName)
    {
        var normalized = tagName.Trim();
        return GitTagNameValidator.IsValidTagName(normalized)
            ? normalized
            : throw new ArgumentException("Tag name is not valid.", nameof(tagName));
    }
}
