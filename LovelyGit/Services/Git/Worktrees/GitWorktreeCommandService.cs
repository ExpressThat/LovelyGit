using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Worktrees;

namespace ExpressThat.LovelyGit.Services.Git.Worktrees;

internal sealed class GitWorktreeCommandService
{
    private readonly GitOperationService _operations;

    public GitWorktreeCommandService(GitOperationService operations)
    {
        _operations = operations;
    }

    public async Task CreateAsync(
        string repositoryPath,
        string worktreePath,
        string branchName,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var destination = NormalizePath(worktreePath);
        var branch = branchName.Trim();
        if (!GitBranchNameValidator.IsValidBranchName(branch))
        {
            throw new ArgumentException("Branch name is not valid.", nameof(branchName));
        }

        if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
        {
            throw new InvalidOperationException("The worktree destination must be empty.");
        }

        var existing = await GitWorktreeReader
            .ReadAsync(paths.WorktreeGitDirectory, paths.WorkTreeDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (existing.Any(worktree => PathsEqual(worktree.Path, destination)))
        {
            throw new InvalidOperationException("This folder is already a repository worktree.");
        }

        await _operations.ExecuteRequiredBufferedAsync(
            "Create worktree",
            ["worktree", "add", "--", destination, branch],
            paths.WorkTreeDirectory,
            "Choose an empty folder and a local branch that is not checked out in another worktree.",
            cancellationToken).ConfigureAwait(false);
    }

    public Task LockAsync(
        string repositoryPath,
        string worktreePath,
        string? reason,
        CancellationToken cancellationToken) =>
        MutateExistingAsync(
            repositoryPath,
            worktreePath,
            "Lock worktree",
            target => string.IsNullOrWhiteSpace(reason)
                ? ["worktree", "lock", "--", target]
                : ["worktree", "lock", "--reason", reason.Trim(), "--", target],
            "Unlock the worktree before trying to lock it again.",
            allowCurrent: false,
            cancellationToken);

    public Task UnlockAsync(
        string repositoryPath,
        string worktreePath,
        CancellationToken cancellationToken) =>
        MutateExistingAsync(
            repositoryPath,
            worktreePath,
            "Unlock worktree",
            target => ["worktree", "unlock", "--", target],
            "The worktree may already be unlocked or may no longer exist.",
            allowCurrent: false,
            cancellationToken);

    public Task RemoveAsync(
        string repositoryPath,
        string worktreePath,
        bool force,
        CancellationToken cancellationToken) =>
        MutateExistingAsync(
            repositoryPath,
            worktreePath,
            "Remove worktree",
            target => force
                ? ["worktree", "remove", "--force", "--", target]
                : ["worktree", "remove", "--", target],
            force
                ? "Unlock the worktree and close programs using its files, then try again."
                : "Commit or discard changes in the worktree, or explicitly choose force removal.",
            allowCurrent: false,
            cancellationToken);

    public async Task<string> ValidateExistingAsync(
        string repositoryPath,
        string worktreePath,
        bool allowCurrent,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var target = NormalizePath(worktreePath);
        var worktrees = await GitWorktreeReader
            .ReadAsync(paths.WorktreeGitDirectory, paths.WorkTreeDirectory, cancellationToken)
            .ConfigureAwait(false);
        var worktree = worktrees.FirstOrDefault(candidate => PathsEqual(candidate.Path, target))
            ?? throw new InvalidOperationException("The selected worktree no longer exists.");
        if (!allowCurrent && worktree.IsCurrent)
        {
            throw new InvalidOperationException("The current worktree cannot be changed from this action.");
        }

        return target;
    }

    private async Task MutateExistingAsync(
        string repositoryPath,
        string worktreePath,
        string operationName,
        Func<string, IReadOnlyList<string>> createArguments,
        string recoveryHint,
        bool allowCurrent,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var target = await ValidateExistingAsync(
            repositoryPath, worktreePath, allowCurrent, cancellationToken).ConfigureAwait(false);
        await _operations.ExecuteRequiredBufferedAsync(
            operationName,
            createArguments(target),
            paths.WorkTreeDirectory,
            recoveryHint,
            cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Worktree path is required.", nameof(path));
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path.Trim()));
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
