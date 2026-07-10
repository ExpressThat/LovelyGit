using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Bisect;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Bisect;

internal sealed class GetBisectStateCommandResolver
    : CommandResponder<GetBisectStateCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly NativeGitBisectStateReader _reader;

    public GetBisectStateCommandResolver(
        KnownGitRepositorysRepository knownRepositories,
        NativeGitBisectStateReader reader)
    {
        _knownRepositories = knownRepositories;
        _reader = reader;
    }

    protected override JsonTypeInfo<GetBisectStateCommandArguments> ArgumentsJsonTypeInfo =>
        BisectJsonSerializerContext.Default.GetBisectStateCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetBisectState;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetBisectStateCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        try
        {
            KnownGitRepository repository = await _knownRepositories
                .FindByIdAsync(command.Arguments.RepositoryId);
            if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
            {
                return Failure(command, "Known repository not found.");
            }

            var state = await _reader
                .ReadAsync(repository.Path, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<GitBisectState>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = state,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetBisectStateCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
