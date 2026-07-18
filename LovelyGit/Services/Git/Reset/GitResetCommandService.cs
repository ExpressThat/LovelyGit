using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.Reset;

internal sealed class GitResetCommandService
{
    private readonly GitOperationService _operations;

    public GitResetCommandService(GitOperationService operations)
    {
        _operations = operations;
    }

    public async Task ResetCurrentBranchToCommitAsync(
        string repositoryPath,
        string commitHash,
        GitResetMode mode,
        CancellationToken cancellationToken)
    {
        var normalizedCommitHash = NormalizeCommitHash(commitHash);
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var branchName = await EnsureResetAllowedAsync(paths, cancellationToken)
            .ConfigureAwait(false);

        await _operations.ExecuteRequiredBufferedAsync(
            $"{FormatMode(mode)} reset {branchName}",
            BuildResetArguments(mode, normalizedCommitHash),
            paths.WorkTreeDirectory,
            RecoveryHint(mode),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UndoLastCommitAsync(
        string repositoryPath,
        string currentCommitHash,
        string parentCommitHash,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var branchName = await EnsureResetAllowedAsync(paths, cancellationToken)
            .ConfigureAwait(false);
        await _operations.ExecuteRequiredBufferedAsync(
            $"Undo last commit on {branchName}",
            [
                "update-ref",
                "-m",
                "reset: undo last commit",
                "HEAD",
                NormalizeCommitHash(parentCommitHash),
                NormalizeCommitHash(currentCommitHash),
            ],
            paths.WorkTreeDirectory,
            "Inspect the staged changes and branch reflog before trying the undo again.",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> EnsureResetAllowedAsync(
        GitRepositoryPaths paths,
        CancellationToken cancellationToken)
    {
        if (GitRepositoryOperationStateReader.Read(paths.WorktreeGitDirectory) is { } operation)
        {
            throw new InvalidOperationException(
                $"Finish or abort the active {FormatOperation(operation)} before resetting.");
        }

        var branchName = await GitRefReader
            .ResolveHeadBranchNameAsync(paths.WorktreeGitDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (branchName is null)
        {
            throw new InvalidOperationException(
                "Check out a local branch before resetting to a commit.");
        }

        return branchName;
    }

    private static string ModeArgument(GitResetMode mode) => mode switch
    {
        GitResetMode.Soft => "--soft",
        GitResetMode.Mixed => "--mixed",
        GitResetMode.Hard => "--hard",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    internal static IReadOnlyList<string> BuildResetArguments(
        GitResetMode mode,
        string commitHash)
    {
        var modeArgument = ModeArgument(mode);
        return mode == GitResetMode.Mixed
            ? ["reset", modeArgument, "--no-refresh", commitHash]
            : ["reset", modeArgument, commitHash];
    }

    private static string RecoveryHint(GitResetMode mode) => mode == GitResetMode.Hard
        ? "Use the branch reflog to locate the previous commit if recovery is required."
        : "Inspect the working tree and branch reflog before trying the reset again.";

    private static string FormatMode(GitResetMode mode) => mode switch
    {
        GitResetMode.Soft => "Soft",
        GitResetMode.Mixed => "Mixed",
        GitResetMode.Hard => "Hard",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static string FormatOperation(GitRepositoryOperationKind operation) =>
        operation == GitRepositoryOperationKind.CherryPick
            ? "cherry-pick"
            : operation.ToString().ToLowerInvariant();

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
}
