using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Platform;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

[TypeSharp]
public record RevealKnownGitRepositoryCommandArguments
{
    public Guid KnownRepositoryId { get; set; }
}

internal sealed class RevealKnownGitRepositoryCommandResolver
    : CommandResponder<RevealKnownGitRepositoryCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositorysRepository;
    private readonly RepositoryRevealService _revealService;

    protected override JsonTypeInfo<RevealKnownGitRepositoryCommandArguments> ArgumentsJsonTypeInfo =>
        KnownRepositoriesJsonSerializerContext.Default.RevealKnownGitRepositoryCommandArguments;

    public RevealKnownGitRepositoryCommandResolver(
        KnownGitRepositorysRepository repositorysRepository,
        RepositoryRevealService revealService)
    {
        _repositorysRepository = repositorysRepository;
        _revealService = revealService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.RevealKnownGitRepository;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<RevealKnownGitRepositoryCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.KnownRepositoryId == Guid.Empty)
        {
            return Failure(command, "KnownRepositoryId is required.");
        }

        try
        {
            KnownGitRepository repository =
                await _repositorysRepository.FindByIdAsync(arguments.KnownRepositoryId);
            if (string.IsNullOrWhiteSpace(repository.Path))
            {
                return Failure(command, "Known repository path is missing.");
            }

            await _revealService.RevealAsync(repository.Path);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<RevealKnownGitRepositoryCommandArguments> command)
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
        NativeCommand<RevealKnownGitRepositoryCommandArguments> command,
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
