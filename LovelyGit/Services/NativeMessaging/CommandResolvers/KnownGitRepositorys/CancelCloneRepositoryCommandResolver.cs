using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

internal sealed class CancelCloneRepositoryCommandResolver
    : CommandResponder<CancelCloneRepositoryCommandArguments>
{
    private readonly GitCloneService _cloneService;

    public CancelCloneRepositoryCommandResolver(GitCloneService cloneService)
    {
        _cloneService = cloneService;
    }

    protected override JsonTypeInfo<CancelCloneRepositoryCommandArguments> ArgumentsJsonTypeInfo =>
        KnownRepositoriesJsonSerializerContext.Default.CancelCloneRepositoryCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CancelCloneRepository;

    public override Task<CommandResponseBase> Resolve(
        NativeCommand<CancelCloneRepositoryCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.OperationId == Guid.Empty)
        {
            return Task.FromResult(Failure(command, "OperationId is required."));
        }

        _cloneService.Cancel(command.Arguments.OperationId);
        return Task.FromResult<CommandResponseBase>(new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        });
    }

    private static CommandResponseBase Failure(
        NativeCommand<CancelCloneRepositoryCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
