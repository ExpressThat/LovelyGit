using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.Stashes;

internal sealed class GitStashCommandService
{
    private readonly GitOperationService _gitOperationService;

    public GitStashCommandService(GitOperationService gitOperationService)
    {
        _gitOperationService = gitOperationService;
    }

    public async Task StashChangesAsync(
        string repositoryPath,
        string message,
        bool includeUntracked,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Stash message is required.", nameof(message));
        }

        var repositoryPaths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);

        if (!includeUntracked)
        {
            await StashTrackedChangesAsync(
                repositoryPaths.WorkTreeDirectory,
                message.Trim(),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await _gitOperationService.ExecuteRequiredBufferedAsync(
            "Create stash",
            ["-c", "gc.auto=0", "stash", "push", "--include-untracked", "-m", message.Trim()],
            repositoryPaths.WorkTreeDirectory,
            "Check that the working tree still has changes before stashing.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task StashTrackedChangesAsync(
        string workingDirectory,
        string message,
        CancellationToken cancellationToken)
    {
        var changedPaths = await GetTrackedChangedPathspecAsync(workingDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (changedPaths.Length == 0)
        {
            throw new InvalidOperationException("There are no tracked changes to stash.");
        }

        var createResult = await _gitOperationService.ExecuteRequiredBufferedAsync(
            "Create tracked stash object",
            ["-c", "gc.auto=0", "stash", "create", message],
            workingDirectory,
            "Check that the working tree still has tracked changes before stashing.",
            cancellationToken).ConfigureAwait(false);
        var stashHash = createResult.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(stashHash))
        {
            throw new InvalidOperationException("Git did not create a stash object.");
        }

        await _gitOperationService.ExecuteRequiredBufferedAsync(
            "Store tracked stash object",
            ["stash", "store", "-m", message, stashHash],
            workingDirectory,
            "Check that the generated stash object still exists.",
            cancellationToken).ConfigureAwait(false);
        await RestoreTrackedPathsAsync(workingDirectory, changedPaths, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<string> GetTrackedChangedPathspecAsync(
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var result = await _gitOperationService.ExecuteRequiredBufferedAsync(
            "List tracked changes",
            ["diff", "--name-only", "-z", "HEAD", "--"],
            workingDirectory,
            "Check that the repository has an initial commit.",
            cancellationToken).ConfigureAwait(false);
        return result.StandardOutput;
    }

    private async Task RestoreTrackedPathsAsync(
        string workingDirectory,
        string changedPaths,
        CancellationToken cancellationToken)
    {
        var pathspecFile = Path.Combine(Path.GetTempPath(), $"lovelygit-pathspec-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(pathspecFile, changedPaths, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await _gitOperationService.ExecuteRequiredBufferedAsync(
                "Restore stashed tracked paths",
                [
                    "restore",
                    "--staged",
                    "--worktree",
                    "--pathspec-file-nul",
                    $"--pathspec-from-file={pathspecFile}",
                ],
                workingDirectory,
                "Apply the stash or inspect the working tree before retrying.",
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            File.Delete(pathspecFile);
        }
    }
}
