using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed class GitBranchIntegrationService
{
    private readonly GitOperationService _gitOperationService;

    public GitBranchIntegrationService(GitOperationService gitOperationService)
    {
        _gitOperationService = gitOperationService;
    }

    public Task<GitBranchIntegrationOutcome> MergeAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            GitRepositoryOperationKind.Merge,
            "Merge branch",
            ["merge", "--no-edit", "--", NormalizeBranchName(branchName)],
            "Resolve and stage merge conflicts, then continue or abort the merge.",
            cancellationToken);

    public Task<GitBranchIntegrationOutcome> RebaseAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            GitRepositoryOperationKind.Rebase,
            "Rebase branch",
            ["rebase", "--", NormalizeBranchName(branchName)],
            "Resolve and stage rebase conflicts, then continue or abort the rebase.",
            cancellationToken);

    public async Task<GitBranchIntegrationOutcome> ContinueAsync(
        string repositoryPath,
        GitRepositoryOperationKind expectedOperation,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        EnsureExpectedOperation(paths.GitDirectory, expectedOperation);
        var arguments = expectedOperation switch
        {
            GitRepositoryOperationKind.Merge => new[] { "-c", "core.editor=true", "merge", "--continue" },
            GitRepositoryOperationKind.Rebase => ["-c", "core.editor=true", "rebase", "--continue"],
            _ => throw new ArgumentOutOfRangeException(nameof(expectedOperation)),
        };

        return await ExecuteAsync(
            paths,
            expectedOperation,
            $"Continue {expectedOperation.ToString().ToLowerInvariant()}",
            arguments,
            "Resolve and stage every conflict before continuing.",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task AbortAsync(
        string repositoryPath,
        GitRepositoryOperationKind expectedOperation,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        EnsureExpectedOperation(paths.GitDirectory, expectedOperation);
        var arguments = expectedOperation switch
        {
            GitRepositoryOperationKind.Merge => new[] { "merge", "--abort" },
            GitRepositoryOperationKind.Rebase => ["rebase", "--abort"],
            _ => throw new ArgumentOutOfRangeException(nameof(expectedOperation)),
        };

        await _gitOperationService.ExecuteRequiredBufferedAsync(
            $"Abort {expectedOperation.ToString().ToLowerInvariant()}",
            arguments,
            paths.WorkTreeDirectory,
            "Use the repository terminal to inspect and recover the operation state.",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitRepositoryOperationKind?> GetOperationAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        return GitRepositoryOperationStateReader.Read(paths.GitDirectory);
    }

    private async Task<GitBranchIntegrationOutcome> RunAsync(
        string repositoryPath,
        GitRepositoryOperationKind operation,
        string operationName,
        IReadOnlyList<string> arguments,
        string recoveryHint,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (GitRepositoryOperationStateReader.Read(paths.GitDirectory) is { } activeOperation)
        {
            throw new InvalidOperationException(
                $"A {activeOperation.ToString().ToLowerInvariant()} is already in progress.");
        }

        return await ExecuteAsync(
            paths,
            operation,
            operationName,
            arguments,
            recoveryHint,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<GitBranchIntegrationOutcome> ExecuteAsync(
        GitRepositoryPaths paths,
        GitRepositoryOperationKind operation,
        string operationName,
        IReadOnlyList<string> arguments,
        string recoveryHint,
        CancellationToken cancellationToken)
    {
        var result = await _gitOperationService.ExecuteBufferedAsync(
            operationName,
            arguments,
            paths.WorkTreeDirectory,
            recoveryHint,
            cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return new GitBranchIntegrationOutcome(IsCompleted: true, Operation: null, Message: null);
        }

        var exception = new GitOperationException(result);
        var activeOperation = GitRepositoryOperationStateReader.Read(paths.GitDirectory);
        if (activeOperation == operation)
        {
            return new GitBranchIntegrationOutcome(
                IsCompleted: false,
                Operation: activeOperation,
                Message: exception.Message);
        }

        throw exception;
    }

    private static void EnsureExpectedOperation(
        string gitDirectory,
        GitRepositoryOperationKind expectedOperation)
    {
        var activeOperation = GitRepositoryOperationStateReader.Read(gitDirectory);
        if (activeOperation != expectedOperation)
        {
            throw new InvalidOperationException(
                activeOperation is null
                    ? "No merge or rebase is currently in progress."
                    : $"A {activeOperation.Value.ToString().ToLowerInvariant()} is in progress instead.");
        }
    }

    private static string NormalizeBranchName(string branchName)
    {
        var normalized = branchName.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Branch name is required.", nameof(branchName));
        }

        if (normalized.Length > 255 || normalized.Any(char.IsControl) || normalized.StartsWith('-'))
        {
            throw new ArgumentException("Branch name is not valid.", nameof(branchName));
        }

        return normalized;
    }
}

internal sealed record GitBranchIntegrationOutcome(
    bool IsCompleted,
    GitRepositoryOperationKind? Operation,
    string? Message);
