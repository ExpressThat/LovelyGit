using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class CancelFileBlameCommandResolver
    : CommandResponder<CancelFileBlameCommandArguments>
{
    private readonly FileBlameService _blameService;

    public CancelFileBlameCommandResolver(FileBlameService blameService)
    {
        _blameService = blameService;
    }

    protected override JsonTypeInfo<CancelFileBlameCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.CancelFileBlameCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CancelFileBlame;

    public override Task<CommandResponseBase> Resolve(
        NativeCommand<CancelFileBlameCommandArguments> command)
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

        _blameService.Cancel(repositoryId);
        return Task.FromResult<CommandResponseBase>(new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        });
    }
}
