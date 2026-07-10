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
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (GitRepositoryOperationStateReader.Read(paths.GitDirectory) is { } operation)
        {
            throw new InvalidOperationException(
                $"Finish or abort the active {FormatOperation(operation)} before resetting.");
        }

        var branchName = await GitRefReader
            .ResolveHeadBranchNameAsync(paths.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (branchName is null)
        {
            throw new InvalidOperationException(
                "Check out a local branch before resetting to a commit.");
        }

        await _operations.ExecuteRequiredBufferedAsync(
            $"{FormatMode(mode)} reset {branchName}",
            ["reset", ModeArgument(mode), NormalizeCommitHash(commitHash)],
            paths.WorkTreeDirectory,
            RecoveryHint(mode),
            cancellationToken).ConfigureAwait(false);
    }

    private static string ModeArgument(GitResetMode mode) => mode switch
    {
        GitResetMode.Soft => "--soft",
        GitResetMode.Mixed => "--mixed",
        GitResetMode.Hard => "--hard",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

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
