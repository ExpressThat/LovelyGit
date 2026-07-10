using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Configuration;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Configuration;

internal sealed class GetCommitIdentityCommandResolver
    : CommandResponder<GetCommitIdentityCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly NativeGitCommitIdentityReader _reader;

    public GetCommitIdentityCommandResolver(
        KnownGitRepositorysRepository knownRepositories,
        NativeGitCommitIdentityReader reader)
    {
        _knownRepositories = knownRepositories;
        _reader = reader;
    }

    protected override JsonTypeInfo<GetCommitIdentityCommandArguments> ArgumentsJsonTypeInfo =>
        ConfigurationJsonSerializerContext.Default.GetCommitIdentityCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetCommitIdentity;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetCommitIdentityCommandArguments> command)
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

            var identity = await _reader
                .ReadAsync(repository.Path, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<GitCommitIdentity>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = identity,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetCommitIdentityCommandArguments> command,
        string message) => new()
    {
        CommandUniqueId = command.CommandUniqueId,
        CommandType = command.CommandType,
        IsSuccess = false,
        ErrorMessage = message,
    };
}
