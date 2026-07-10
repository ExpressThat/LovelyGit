using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.Patches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class ChoosePatchFileCommandResolver : CommandResponder<EmptyCommandArguments>
{
    private readonly PatchPreviewService _previewService;

    public ChoosePatchFileCommandResolver(PatchPreviewService previewService)
    {
        _previewService = previewService;
    }

    protected override JsonTypeInfo<EmptyCommandArguments> ArgumentsJsonTypeInfo =>
        CommandJsonSerializerContext.Default.EmptyCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ChoosePatchFile;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<EmptyCommandArguments> command)
    {
        try
        {
            var preview = await _previewService
                .ChooseAndReadAsync(CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<PatchPreviewResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = preview,
            };
        }
        catch (Exception exception)
        {
            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = false,
                ErrorMessage = exception.Message,
            };
        }
    }
}
