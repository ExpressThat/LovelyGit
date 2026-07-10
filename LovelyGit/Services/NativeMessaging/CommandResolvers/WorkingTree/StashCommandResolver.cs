using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Stashes;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class StashCommandResolver : CommandResponder<StashCommandArguments>
{
    private readonly GitStashCommandService _stashCommands;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public StashCommandResolver(
        GitStashCommandService stashCommands,
        KnownGitRepositorysRepository knownRepositories)
    {
        _stashCommands = stashCommands;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<StashCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.StashCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ManageStash;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<StashCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository foundRepo = await _knownRepositories.FindByIdAsync(arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _stashCommands.ExecuteAsync(
                foundRepo.Path,
                arguments.Action,
                arguments.Selector,
                arguments.Message,
                arguments.IncludeUntracked,
                arguments.RestoreIndex,
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(NativeCommand<StashCommandArguments> command) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };

    private static CommandResponseBase Failure(
        NativeCommand<StashCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
