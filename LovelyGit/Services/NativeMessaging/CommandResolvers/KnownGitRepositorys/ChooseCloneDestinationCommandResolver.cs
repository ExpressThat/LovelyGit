using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

internal sealed class ChooseCloneDestinationCommandResolver : CommandResponder<EmptyCommandArguments>
{
    private readonly IFolderPicker _folderPicker;

    public ChooseCloneDestinationCommandResolver(IFolderPicker folderPicker)
    {
        _folderPicker = folderPicker;
    }

    protected override JsonTypeInfo<EmptyCommandArguments> ArgumentsJsonTypeInfo =>
        CommandJsonSerializerContext.Default.EmptyCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ChooseCloneDestination;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<EmptyCommandArguments> command)
    {
        var selectedFolder = await _folderPicker
            .PickFolderAsync(CancellationToken.None, "Select clone destination")
            .ConfigureAwait(false);
        return new CommandResponse<CloneDestinationResponse?>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = string.IsNullOrWhiteSpace(selectedFolder)
                ? null
                : new CloneDestinationResponse
                {
                    ParentPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(selectedFolder)),
                },
        };
    }
}
