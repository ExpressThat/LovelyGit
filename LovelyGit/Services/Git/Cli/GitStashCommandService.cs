using System.Text.RegularExpressions;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace ExpressThat.LovelyGit.Services.Git.Stashes;

internal sealed partial class GitStashCommandService
{
    private readonly GitOperationService _gitOperationService;

    public GitStashCommandService(GitOperationService gitOperationService)
    {
        _gitOperationService = gitOperationService;
    }

    public Task ExecuteAsync(
        string repositoryPath,
        StashAction action,
        string? selector,
        string? message,
        bool includeUntracked,
        bool restoreIndex,
        bool selectedOnly,
        IReadOnlyList<string>? paths,
        CancellationToken cancellationToken)
    {
        if (action != StashAction.Create && (selectedOnly || paths is { Count: > 0 }))
        {
            throw new InvalidOperationException("File paths are only valid when creating a stash.");
        }

        if (!selectedOnly && paths is { Count: > 0 })
        {
            throw new InvalidOperationException("Selected stash paths require selected-file scope.");
        }

        var selectedPaths = selectedOnly
            ? GitPathspecs.Normalize(paths ?? [])
            : [];
        var arguments = action switch
        {
            StashAction.Create => BuildCreateArguments(message, includeUntracked, selectedPaths.Count > 0),
            StashAction.Apply => BuildExistingArguments("apply", selector, restoreIndex),
            StashAction.Pop => BuildExistingArguments("pop", selector, restoreIndex),
            StashAction.Drop => BuildExistingArguments("drop", selector, restoreIndex: false),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

        return RunAsync(repositoryPath, action, arguments, selectedPaths, cancellationToken);
    }

    public Task StashChangesAsync(
        string repositoryPath,
        string message,
        bool includeUntracked,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Stash message is required.", nameof(message));
        }

        return ExecuteAsync(
            repositoryPath,
            StashAction.Create,
            selector: null,
            message,
            includeUntracked,
            restoreIndex: false,
            selectedOnly: false,
            paths: null,
            cancellationToken);
    }

    public Task ApplyStashAsync(
        string repositoryPath,
        string selector,
        CancellationToken cancellationToken) =>
        ExecuteAsync(repositoryPath, StashAction.Apply, NormalizeAlias(selector), null, false, false, false, null, cancellationToken);

    public Task PopStashAsync(
        string repositoryPath,
        string selector,
        CancellationToken cancellationToken) =>
        ExecuteAsync(repositoryPath, StashAction.Pop, NormalizeAlias(selector), null, false, false, false, null, cancellationToken);

    public Task DropStashAsync(
        string repositoryPath,
        string selector,
        CancellationToken cancellationToken) =>
        ExecuteAsync(repositoryPath, StashAction.Drop, NormalizeAlias(selector), null, false, false, false, null, cancellationToken);

    public Task BranchFromStashAsync(
        string repositoryPath,
        string? selector,
        string? branchName,
        CancellationToken cancellationToken)
    {
        var normalizedBranchName = branchName?.Trim() ?? string.Empty;
        if (!GitBranchNameValidator.IsValidBranchName(normalizedBranchName))
        {
            throw new ArgumentException("Branch name is not valid.", nameof(branchName));
        }

        var arguments = BuildExistingArguments(
            "branch",
            selector,
            restoreIndex: false).ToList();
        arguments.Insert(2, normalizedBranchName);
        return RunAsync(repositoryPath, StashAction.Branch, arguments, [], cancellationToken);
    }

    private async Task RunAsync(
        string repositoryPath,
        StashAction action,
        IReadOnlyList<string> arguments,
        IReadOnlyList<string> selectedPaths,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var recoveryHint = action is StashAction.Apply or StashAction.Pop or StashAction.Branch
            ? "Resolve any conflicts in the working tree, then continue from there."
            : null;
        if (selectedPaths.Count == 0)
        {
            await _gitOperationService.ExecuteRequiredBufferedAsync(
                $"{action} stash",
                arguments,
                paths.WorkTreeDirectory,
                recoveryHint,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await _gitOperationService.ExecuteRequiredBufferedWithInputAsync(
            $"{action} stash",
            arguments,
            paths.WorkTreeDirectory,
            recoveryHint,
            PipeSource.Create((stream, token) =>
                GitPathspecs.WriteNullTerminatedAsync(stream, selectedPaths, token)),
            cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> BuildCreateArguments(
        string? message,
        bool includeUntracked,
        bool hasSelectedPaths)
    {
        var arguments = new List<string>();
        if (hasSelectedPaths)
        {
            arguments.Add("--literal-pathspecs");
        }

        arguments.Add("stash");
        arguments.Add("push");
        if (includeUntracked)
        {
            arguments.Add("--include-untracked");
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            arguments.Add("--message");
            arguments.Add(message.Trim());
        }

        if (hasSelectedPaths)
        {
            arguments.Add("--pathspec-from-file=-");
            arguments.Add("--pathspec-file-nul");
        }

        return arguments;
    }

    private static IReadOnlyList<string> BuildExistingArguments(
        string operation,
        string? selector,
        bool restoreIndex)
    {
        var normalizedSelector = selector?.Trim() ?? string.Empty;
        if (!StashSelectorRegex().IsMatch(normalizedSelector))
        {
            throw new ArgumentException("Stash selector is not valid.", nameof(selector));
        }

        var arguments = new List<string> { "stash", operation };
        if (restoreIndex)
        {
            arguments.Add("--index");
        }

        arguments.Add(normalizedSelector);
        return arguments;
    }

    [GeneratedRegex("^stash@\\{[0-9]+\\}$", RegexOptions.CultureInvariant)]
    private static partial Regex StashSelectorRegex();

    private static string NormalizeAlias(string selector) =>
        selector.Trim().Equals("stash", StringComparison.Ordinal)
            ? "stash@{0}"
            : selector;
}
