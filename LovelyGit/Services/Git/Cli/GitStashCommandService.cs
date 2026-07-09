using System.Text.RegularExpressions;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

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
        CancellationToken cancellationToken)
    {
        var arguments = action switch
        {
            StashAction.Create => BuildCreateArguments(message, includeUntracked),
            StashAction.Apply => BuildExistingArguments("apply", selector, restoreIndex),
            StashAction.Pop => BuildExistingArguments("pop", selector, restoreIndex),
            StashAction.Drop => BuildExistingArguments("drop", selector, restoreIndex: false),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

        return RunAsync(repositoryPath, action, arguments, cancellationToken);
    }

    private async Task RunAsync(
        string repositoryPath,
        StashAction action,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var paths = await GitRepositoryDiscovery
            .ResolveRepositoryPathsAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        await _gitOperationService.ExecuteRequiredBufferedAsync(
            $"{action} stash",
            arguments,
            paths.WorkTreeDirectory,
            action is StashAction.Apply or StashAction.Pop
                ? "Resolve any conflicts in the working tree, then continue from there."
                : null,
            cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> BuildCreateArguments(
        string? message,
        bool includeUntracked)
    {
        var arguments = new List<string> { "stash", "push" };
        if (includeUntracked)
        {
            arguments.Add("--include-untracked");
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            arguments.Add("--message");
            arguments.Add(message.Trim());
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
}
