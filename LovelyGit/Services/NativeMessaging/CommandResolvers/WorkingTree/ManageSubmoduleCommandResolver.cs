using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Submodules;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class ManageSubmoduleCommandResolver
    : CommandResponder<ManageSubmoduleCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly SubmoduleCommandService _service;

    public ManageSubmoduleCommandResolver(
        KnownGitRepositorysRepository knownRepositories,
        SubmoduleCommandService service)
    {
        _knownRepositories = knownRepositories;
        _service = service;
    }

    protected override JsonTypeInfo<ManageSubmoduleCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.ManageSubmoduleCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ManageSubmodule;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ManageSubmoduleCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository repository = await _knownRepositories
            .FindByIdAsync(command.Arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _service.ExecuteAsync(
                    repository.Path,
                    command.Arguments.Path,
                    command.Arguments.Action,
                    CancellationToken.None)
                .ConfigureAwait(false);
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

    private static CommandResponseBase Failure(
        NativeCommand<ManageSubmoduleCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
