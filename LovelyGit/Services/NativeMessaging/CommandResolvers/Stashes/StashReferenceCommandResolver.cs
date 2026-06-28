using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Stashes;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Stashes;

internal sealed class StashReferenceCommandResolver
    : CommandResponder<StashReferenceCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositorysRepository;
    private readonly GitStashCommandService _stashCommandService;

    protected override JsonTypeInfo<StashReferenceCommandArguments> ArgumentsJsonTypeInfo =>
        StashesJsonSerializerContext.Default.StashReferenceCommandArguments;

    public StashReferenceCommandResolver(
        KnownGitRepositorysRepository repositorysRepository,
        GitStashCommandService stashCommandService)
    {
        _repositorysRepository = repositorysRepository;
        _stashCommandService = stashCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType is NativeMessageType.ApplyStash
            or NativeMessageType.PopStash
            or NativeMessageType.DropStash;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<StashReferenceCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (string.IsNullOrWhiteSpace(arguments.StashName))
        {
            return Failure(command, "Stash reference is required.");
        }

        KnownGitRepository repository = await _repositorysRepository.FindByIdAsync(arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await RunActionAsync(command.CommandType, repository.Path, arguments.StashName)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private async Task RunActionAsync(
        NativeMessageType commandType,
        string repositoryPath,
        string stashName)
    {
        var cancellationToken = CancellationToken.None;
        if (commandType == NativeMessageType.ApplyStash)
        {
            await _stashCommandService.ApplyStashAsync(repositoryPath, stashName, cancellationToken);
        }
        else if (commandType == NativeMessageType.PopStash)
        {
            await _stashCommandService.PopStashAsync(repositoryPath, stashName, cancellationToken);
        }
        else
        {
            await _stashCommandService.DropStashAsync(repositoryPath, stashName, cancellationToken);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<StashReferenceCommandArguments> command) =>
        new CommandResponse<EmptyCommandArguments>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = new EmptyCommandArguments(),
        };

    private static CommandResponseBase Failure(
        NativeCommand<StashReferenceCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
