using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.RemoteSync;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class GetRemoteSyncStatusCommandResolver
    : CommandResponder<GetRemoteSyncStatusCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;

    public GetRemoteSyncStatusCommandResolver(KnownGitRepositorysRepository repositories)
    {
        _repositories = repositories;
    }

    protected override JsonTypeInfo<GetRemoteSyncStatusCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.GetRemoteSyncStatusCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetRemoteSyncStatus;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetRemoteSyncStatusCommandArguments> command)
    {
        if (command.Arguments is not { RepositoryId: var repositoryId } || repositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var known = await _repositories.FindByIdAsync(repositoryId).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(known?.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var result = await NativeRemoteSyncStatusReader
                .ReadAsync(known.Path, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<RemoteSyncStatusResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = result,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetRemoteSyncStatusCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
