using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Platform;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

[TypeSharp]
public record OpenRepositoryTerminalCommandArguments
{
    public Guid KnownRepositoryId { get; set; }
}

internal sealed class OpenRepositoryTerminalCommandResolver
    : CommandResponder<OpenRepositoryTerminalCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositorysRepository;
    private readonly RepositoryTerminalService _terminalService;

    protected override JsonTypeInfo<OpenRepositoryTerminalCommandArguments> ArgumentsJsonTypeInfo =>
        KnownRepositoriesJsonSerializerContext.Default.OpenRepositoryTerminalCommandArguments;

    public OpenRepositoryTerminalCommandResolver(
        KnownGitRepositorysRepository repositorysRepository,
        RepositoryTerminalService terminalService)
    {
        _repositorysRepository = repositorysRepository;
        _terminalService = terminalService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.OpenRepositoryTerminal;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<OpenRepositoryTerminalCommandArguments> command)
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

            await _terminalService.OpenAsync(repository.Path);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<OpenRepositoryTerminalCommandArguments> command)
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
        NativeCommand<OpenRepositoryTerminalCommandArguments> command,
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
