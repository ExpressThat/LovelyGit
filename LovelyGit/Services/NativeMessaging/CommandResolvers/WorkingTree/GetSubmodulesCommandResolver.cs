using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Submodules;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class GetSubmodulesCommandResolver : CommandResponder<GetRemotesCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly NativeSubmoduleReader _reader;

    public GetSubmodulesCommandResolver(
        KnownGitRepositorysRepository knownRepositories,
        NativeSubmoduleReader reader)
    {
        _knownRepositories = knownRepositories;
        _reader = reader;
    }

    protected override JsonTypeInfo<GetRemotesCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.GetRemotesCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetSubmodules;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetRemotesCommandArguments> command)
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
            var result = await _reader.ReadAsync(repository.Path, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<List<GitSubmodule>>
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
        NativeCommand<GetRemotesCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
