using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.OperationState;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.OperationState;

internal sealed class GetGitOperationStateCommandResolver
    : CommandResponder<GetGitOperationStateCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositorysRepository;
    private readonly GitOperationStateService _operationStateService;

    protected override JsonTypeInfo<GetGitOperationStateCommandArguments> ArgumentsJsonTypeInfo =>
        OperationStateJsonSerializerContext.Default.GetGitOperationStateCommandArguments;

    public GetGitOperationStateCommandResolver(
        KnownGitRepositorysRepository repositorysRepository,
        GitOperationStateService operationStateService)
    {
        _repositorysRepository = repositorysRepository;
        _operationStateService = operationStateService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetGitOperationState;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetGitOperationStateCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository repository = await _repositorysRepository.FindByIdAsync(arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        var state = await _operationStateService
            .GetStateAsync(repository.Path, CancellationToken.None)
            .ConfigureAwait(false);
        return new CommandResponse<GitOperationState>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = state,
        };
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetGitOperationStateCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
