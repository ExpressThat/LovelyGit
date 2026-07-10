using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed class GitRepositoryOperationService
{
    private readonly GitOperationService _gitOperationService;

    public GitRepositoryOperationService(GitOperationService gitOperationService)
    {
        _gitOperationService = gitOperationService;
    }

    public Task<GitRepositoryOperationOutcome> MergeAsync(
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

    public Task<GitRepositoryOperationOutcome> CherryPickAsync(
        string repositoryPath,
        string commitHash,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            GitRepositoryOperationKind.CherryPick,
            "Cherry-pick commit",
            ["cherry-pick", "--no-edit", "--", NormalizeCommitHash(commitHash)],
            "Resolve and stage cherry-pick conflicts, then continue or abort the cherry-pick.",
            cancellationToken);

    public Task<GitRepositoryOperationOutcome> RebaseAsync(
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

    public Task<GitRepositoryOperationOutcome> RevertAsync(
        string repositoryPath,
        string commitHash,
        CancellationToken cancellationToken) =>
        RunAsync(
            repositoryPath,
            GitRepositoryOperationKind.Revert,
            "Revert commit",
            ["revert", "--no-edit", "--", NormalizeCommitHash(commitHash)],
            "Resolve and stage revert conflicts, then continue or abort the revert.",
            cancellationToken);

    public async Task<GitRepositoryOperationOutcome> ContinueAsync(
        string repositoryPath,
        GitRepositoryOperationKind expectedOperation,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        EnsureExpectedOperation(paths.WorktreeGitDirectory, expectedOperation);
        var arguments = expectedOperation switch
        {
            GitRepositoryOperationKind.CherryPick =>
                new[] { "-c", "core.editor=true", "cherry-pick", "--continue" },
            GitRepositoryOperationKind.Merge => new[] { "-c", "core.editor=true", "merge", "--continue" },
            GitRepositoryOperationKind.Rebase => ["-c", "core.editor=true", "rebase", "--continue"],
            GitRepositoryOperationKind.Revert =>
                new[] { "-c", "core.editor=true", "revert", "--continue" },
            _ => throw new ArgumentOutOfRangeException(nameof(expectedOperation)),
        };

        return await ExecuteAsync(
            paths,
            expectedOperation,
            $"Continue {FormatOperationName(expectedOperation)}",
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
        EnsureExpectedOperation(paths.WorktreeGitDirectory, expectedOperation);
        var arguments = expectedOperation switch
        {
            GitRepositoryOperationKind.CherryPick => new[] { "cherry-pick", "--abort" },
            GitRepositoryOperationKind.Merge => new[] { "merge", "--abort" },
            GitRepositoryOperationKind.Rebase => ["rebase", "--abort"],
            GitRepositoryOperationKind.Revert => new[] { "revert", "--abort" },
            _ => throw new ArgumentOutOfRangeException(nameof(expectedOperation)),
        };

        await _gitOperationService.ExecuteRequiredBufferedAsync(
            $"Abort {FormatOperationName(expectedOperation)}",
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
        return GitRepositoryOperationStateReader.Read(paths.WorktreeGitDirectory);
    }

    private async Task<GitRepositoryOperationOutcome> RunAsync(
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
        if (GitRepositoryOperationStateReader.Read(paths.WorktreeGitDirectory) is { } activeOperation)
        {
            throw new InvalidOperationException(
                $"A {FormatOperationName(activeOperation)} is already in progress.");
        }

        return await ExecuteAsync(
            paths,
            operation,
            operationName,
            arguments,
            recoveryHint,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<GitRepositoryOperationOutcome> ExecuteAsync(
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
            return new GitRepositoryOperationOutcome(IsCompleted: true, Operation: null, Message: null);
        }

        var exception = new GitOperationException(result);
        var activeOperation = GitRepositoryOperationStateReader.Read(paths.WorktreeGitDirectory);
        if (activeOperation == operation)
        {
            return new GitRepositoryOperationOutcome(
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
                    ? "No cherry-pick, merge, rebase, or revert is currently in progress."
                    : $"A {FormatOperationName(activeOperation.Value)} is in progress instead.");
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

    private static string FormatOperationName(GitRepositoryOperationKind operation) =>
        operation == GitRepositoryOperationKind.CherryPick
            ? "cherry-pick"
            : operation.ToString().ToLowerInvariant();
}

internal sealed record GitRepositoryOperationOutcome(
    bool IsCompleted,
    GitRepositoryOperationKind? Operation,
    string? Message);
