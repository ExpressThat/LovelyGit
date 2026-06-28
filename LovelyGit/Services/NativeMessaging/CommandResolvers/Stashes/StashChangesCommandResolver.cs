using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Stashes;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Stashes;

internal sealed class StashChangesCommandResolver
    : CommandResponder<StashChangesCommandArguments>
{
    private readonly GitStashCommandService _stashCommandService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<StashChangesCommandArguments> ArgumentsJsonTypeInfo =>
        StashesJsonSerializerContext.Default.StashChangesCommandArguments;

    public StashChangesCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitStashCommandService stashCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _stashCommandService = stashCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.StashChanges;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<StashChangesCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (string.IsNullOrWhiteSpace(arguments.Message))
        {
            return Failure(command, "Stash message is required.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _stashCommandService.StashChangesAsync(
                foundRepo.Path,
                arguments.Message,
                arguments.IncludeUntracked,
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<StashChangesCommandArguments> command)
    {
        return new CommandResponse<EmptyCommandArguments>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = new EmptyCommandArguments(),
        };
    }

    private static CommandResponseBase Failure(
        NativeCommand<StashChangesCommandArguments> command,
        string errorMessage)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
    }
}
