using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class ChooseWorktreeDestinationCommandResolver
    : CommandResponder<EmptyCommandArguments>
{
    private readonly IFolderPicker _folderPicker;

    public ChooseWorktreeDestinationCommandResolver(IFolderPicker folderPicker)
    {
        _folderPicker = folderPicker;
    }

    protected override JsonTypeInfo<EmptyCommandArguments> ArgumentsJsonTypeInfo =>
        CommandJsonSerializerContext.Default.EmptyCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ChooseWorktreeDestination;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<EmptyCommandArguments> command)
    {
        var selected = await _folderPicker
            .PickFolderAsync(CancellationToken.None, "Select an empty worktree folder")
            .ConfigureAwait(false);
        return new CommandResponse<WorktreeDestinationResponse?>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = string.IsNullOrWhiteSpace(selected)
                ? null
                : new WorktreeDestinationResponse
                {
                    Path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(selected)),
                },
        };
    }
}
