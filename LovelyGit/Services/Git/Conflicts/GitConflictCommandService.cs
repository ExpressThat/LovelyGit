using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.OperationState;

namespace ExpressThat.LovelyGit.Services.Git.Conflicts;

internal sealed class GitConflictCommandService
{
    private readonly GitOperationService _gitOperationService;
    private readonly GitOperationStateService _operationStateService;

    public GitConflictCommandService(
        GitOperationService gitOperationService,
        GitOperationStateService operationStateService)
    {
        _gitOperationService = gitOperationService;
        _operationStateService = operationStateService;
    }

    public async Task ResolveFileAsync(
        string repositoryPath,
        string path,
        GitConflictAction action,
        CancellationToken cancellationToken)
    {
        path = ValidatePath(path);
        var workTreeDirectory = await ResolveWorkTreeAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (action == GitConflictAction.UseOurs)
        {
            await RunAsync("Use ours", ["checkout", "--ours", "--", path], workTreeDirectory, cancellationToken)
                .ConfigureAwait(false);
        }
        else if (action == GitConflictAction.UseTheirs)
        {
            await RunAsync("Use theirs", ["checkout", "--theirs", "--", path], workTreeDirectory, cancellationToken)
                .ConfigureAwait(false);
        }

        await RunAsync("Mark conflict resolved", ["add", "--", path], workTreeDirectory, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ContinueAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var operation = await _operationStateService
            .GetStateAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var workTreeDirectory = await ResolveWorkTreeAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var arguments = operation.Kind switch
        {
            GitOperationKind.Merge => new[] { "-c", "core.editor=true", "merge", "--continue" },
            GitOperationKind.Rebase => new[] { "-c", "core.editor=true", "rebase", "--continue" },
            GitOperationKind.CherryPick => new[] { "-c", "core.editor=true", "cherry-pick", "--continue" },
            GitOperationKind.Revert => new[] { "-c", "core.editor=true", "revert", "--continue" },
            _ => throw new InvalidOperationException("No continuable Git operation is in progress."),
        };

        await RunAsync("Continue operation", arguments, workTreeDirectory, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AbortAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var operation = await _operationStateService
            .GetStateAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var workTreeDirectory = await ResolveWorkTreeAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var arguments = operation.Kind switch
        {
            GitOperationKind.Merge => new[] { "merge", "--abort" },
            GitOperationKind.Rebase => new[] { "rebase", "--abort" },
            GitOperationKind.CherryPick => new[] { "cherry-pick", "--abort" },
            GitOperationKind.Revert => new[] { "revert", "--abort" },
            _ => throw new InvalidOperationException("No abortable Git operation is in progress."),
        };

        await RunAsync("Abort operation", arguments, workTreeDirectory, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task RunAsync(
        string operationName,
        IReadOnlyList<string> arguments,
        string workTreeDirectory,
        CancellationToken cancellationToken)
    {
        await _gitOperationService.ExecuteRequiredBufferedAsync(
                operationName,
                arguments,
                workTreeDirectory,
                "Refresh the conflict view and check the repository state.",
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<string> ResolveWorkTreeAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        return paths.WorkTreeDirectory;
    }

    private static string ValidatePath(string path)
    {
        path = path.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(path) || path.Contains("../", StringComparison.Ordinal))
        {
            throw new ArgumentException("Conflict path is not valid.", nameof(path));
        }

        return path;
    }
}
