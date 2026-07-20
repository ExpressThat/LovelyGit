using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Worktrees;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Platform;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class ManageWorktreeCommandResolver : CommandResponder<ManageWorktreeCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly RepositoryRevealService _reveal;
    private readonly RepositoryTerminalService _terminal;
    private readonly GitWorktreeCommandService _worktrees;

    public ManageWorktreeCommandResolver(
        KnownGitRepositorysRepository repositories,
        RepositoryRevealService reveal,
        RepositoryTerminalService terminal,
        GitWorktreeCommandService worktrees)
    {
        _repositories = repositories;
        _reveal = reveal;
        _terminal = terminal;
        _worktrees = worktrees;
    }

    protected override JsonTypeInfo<ManageWorktreeCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.ManageWorktreeCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ManageWorktree;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ManageWorktreeCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var repository = await _repositories.FindByIdAsync(arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var openedRepository = await RunAsync(repository.Path, arguments).ConfigureAwait(false);
            return new CommandResponse<KnownGitRepository?>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = openedRepository,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private async Task<KnownGitRepository?> RunAsync(
        string repositoryPath,
        ManageWorktreeCommandArguments arguments)
    {
        switch (arguments.Action)
        {
            case WorktreeMutationAction.Open:
                var openTarget = await ValidateAsync(repositoryPath, arguments, allowCurrent: true)
                    .ConfigureAwait(false);
                return await RegisterAsync(openTarget).ConfigureAwait(false);
            case WorktreeMutationAction.Reveal:
                await _reveal.RevealAsync(
                    await ValidateAsync(repositoryPath, arguments, allowCurrent: true)
                        .ConfigureAwait(false)).ConfigureAwait(false);
                return null;
            case WorktreeMutationAction.Terminal:
                await _terminal.OpenAsync(
                    await ValidateAsync(repositoryPath, arguments, allowCurrent: true)
                        .ConfigureAwait(false)).ConfigureAwait(false);
                return null;
            case WorktreeMutationAction.Lock:
                await _worktrees.LockAsync(
                    repositoryPath,
                    arguments.WorktreePath,
                    arguments.LockReason,
                    CancellationToken.None).ConfigureAwait(false);
                return null;
            case WorktreeMutationAction.Unlock:
                await _worktrees.UnlockAsync(
                    repositoryPath,
                    arguments.WorktreePath,
                    CancellationToken.None).ConfigureAwait(false);
                return null;
            case WorktreeMutationAction.Remove:
                var removeTarget = await ValidateAsync(repositoryPath, arguments, allowCurrent: false)
                    .ConfigureAwait(false);
                await _worktrees.RemoveAsync(
                    repositoryPath,
                    removeTarget,
                    arguments.Force,
                    CancellationToken.None).ConfigureAwait(false);
                var registered = await _repositories.FindByPathAsync(removeTarget).ConfigureAwait(false);
                if (registered != null)
                {
                    await _repositories.RemoveAsync(registered.Id).ConfigureAwait(false);
                }
                return registered;
            default:
                throw new ArgumentOutOfRangeException(nameof(arguments), "Worktree action is not supported.");
        }
    }

    private Task<string> ValidateAsync(
        string repositoryPath,
        ManageWorktreeCommandArguments arguments,
        bool allowCurrent) =>
        _worktrees.ValidateExistingAsync(
            repositoryPath,
            arguments.WorktreePath,
            allowCurrent,
            CancellationToken.None);

    private async Task<KnownGitRepository> RegisterAsync(string path)
    {
        var existing = await _repositories.FindByPathAsync(path).ConfigureAwait(false);
        if (existing != null)
        {
            return existing;
        }

        return await _repositories.AddAsync(new KnownGitRepository
        {
            Id = Guid.NewGuid(),
            Name = new DirectoryInfo(path).Name,
            Path = path,
        }).ConfigureAwait(false);
    }

    private static CommandResponseBase Failure(
        NativeCommand<ManageWorktreeCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
