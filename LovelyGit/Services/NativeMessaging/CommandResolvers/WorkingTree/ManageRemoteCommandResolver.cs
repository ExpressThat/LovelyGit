using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class ManageRemoteCommandResolver : CommandResponder<ManageRemoteCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly GitRemoteCommandService _commands;

    protected override JsonTypeInfo<ManageRemoteCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.ManageRemoteCommandArguments;

    public ManageRemoteCommandResolver(
        KnownGitRepositorysRepository repositories,
        GitRemoteCommandService commands)
    {
        _repositories = repositories;
        _commands = commands;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ManageRemote;

    public override async Task<CommandResponseBase> Resolve(NativeCommand<ManageRemoteCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var repository = await _repositories.FindByIdAsync(arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await RunAsync(repository.Path, arguments).ConfigureAwait(false);
            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private Task RunAsync(string path, ManageRemoteCommandArguments arguments) =>
        arguments.Action switch
        {
            RemoteMutationAction.Add => _commands.AddAsync(
                path, arguments.Name, arguments.Url ?? string.Empty, arguments.PushUrl, CancellationToken.None),
            RemoteMutationAction.Update => _commands.UpdateAsync(
                path,
                arguments.Name,
                arguments.NewName ?? arguments.Name,
                arguments.Url ?? string.Empty,
                arguments.PushUrl,
                CancellationToken.None),
            RemoteMutationAction.Remove => _commands.RemoveAsync(
                path, arguments.Name, CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(arguments), "Remote action is not supported."),
        };

    private static CommandResponseBase Failure(
        NativeCommand<ManageRemoteCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
