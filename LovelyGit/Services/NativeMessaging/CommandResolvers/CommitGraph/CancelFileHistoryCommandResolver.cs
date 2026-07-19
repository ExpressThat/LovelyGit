using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.FileHistory;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class CancelFileHistoryCommandResolver
    : CommandResponder<CancelFileHistoryCommandArguments>
{
    private readonly FileHistoryService _historyService;

    public CancelFileHistoryCommandResolver(FileHistoryService historyService)
    {
        _historyService = historyService;
    }

    protected override JsonTypeInfo<CancelFileHistoryCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.CancelFileHistoryCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CancelFileHistory;

    public override Task<CommandResponseBase> Resolve(
        NativeCommand<CancelFileHistoryCommandArguments> command)
    {
        if (command.Arguments is not { KnownRepositoryId: var repositoryId }
            || repositoryId == Guid.Empty)
        {
            return Task.FromResult<CommandResponseBase>(new()
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = false,
                ErrorMessage = "KnownRepositoryId is required.",
            });
        }

        _historyService.Cancel(repositoryId);
        return Task.FromResult<CommandResponseBase>(new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        });
    }
}
