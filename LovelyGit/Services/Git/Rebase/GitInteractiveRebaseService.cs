using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

namespace ExpressThat.LovelyGit.Services.Git.Rebase;

internal sealed class GitInteractiveRebaseService
{
    private const string RecoveryHint =
        "Resolve and stage rebase conflicts, then continue or abort the rebase.";
    private readonly GitOperationService _operations;
    private readonly Func<string, string> _sequenceEditorCommandFactory;

    public GitInteractiveRebaseService(GitOperationService operations)
        : this(operations, InteractiveRebaseSequenceEditor.CreateCommand)
    {
    }

    internal GitInteractiveRebaseService(
        GitOperationService operations,
        Func<string, string> sequenceEditorCommandFactory)
    {
        _operations = operations;
        _sequenceEditorCommandFactory = sequenceEditorCommandFactory;
    }

    public async Task<GitRepositoryOperationOutcome> StartAsync(
        string repositoryPath,
        string baseCommitHash,
        IReadOnlyList<InteractiveRebasePlanItem> plan,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            repositoryPath, cancellationToken).ConfigureAwait(false);
        if (GitRepositoryOperationStateReader.Read(paths.WorktreeGitDirectory) is { } active)
        {
            throw new InvalidOperationException($"A {active.ToString().ToLowerInvariant()} is already in progress.");
        }

        var current = await NativeInteractiveRebasePlanReader.ReadAsync(
            repositoryPath, baseCommitHash, cancellationToken).ConfigureAwait(false);
        InteractiveRebasePlanValidator.Validate(current, plan);
        var workingDirectory = CreateWorkingDirectory(paths.WorktreeGitDirectory);
        try
        {
            var todoPath = await InteractiveRebaseTodoWriter.WriteAsync(
                workingDirectory, current, plan, cancellationToken).ConfigureAwait(false);
            var result = await _operations.ExecuteBufferedWithEnvironmentAsync(
                "Interactive rebase",
                ["rebase", "-i", "--", current.BaseCommitHash],
                paths.WorkTreeDirectory,
                RecoveryHint,
                new Dictionary<string, string?>
                {
                    ["GIT_SEQUENCE_EDITOR"] = _sequenceEditorCommandFactory(todoPath),
                    ["GIT_EDITOR"] = "true",
                },
                cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                DeleteDirectory(workingDirectory);
                return new GitRepositoryOperationOutcome(true, null, null);
            }

            var exception = new GitOperationException(result);
            if (GitRepositoryOperationStateReader.Read(paths.WorktreeGitDirectory) ==
                GitRepositoryOperationKind.Rebase)
            {
                return new GitRepositoryOperationOutcome(
                    false, GitRepositoryOperationKind.Rebase, exception.Message);
            }

            DeleteDirectory(workingDirectory);
            throw exception;
        }
        catch
        {
            if (GitRepositoryOperationStateReader.Read(paths.WorktreeGitDirectory) !=
                GitRepositoryOperationKind.Rebase)
            {
                DeleteDirectory(workingDirectory);
            }

            throw;
        }
    }

    public static void Cleanup(string worktreeGitDirectory)
    {
        var root = Path.Combine(worktreeGitDirectory, "lovelygit", "rebase");
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private static string CreateWorkingDirectory(string gitDirectory) =>
        Path.Combine(gitDirectory, "lovelygit", "rebase", Guid.NewGuid().ToString("N"));

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
