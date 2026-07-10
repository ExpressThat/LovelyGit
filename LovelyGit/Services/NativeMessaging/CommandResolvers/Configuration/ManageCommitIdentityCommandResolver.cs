using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Configuration;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Configuration;

internal sealed class ManageCommitIdentityCommandResolver
    : CommandResponder<ManageCommitIdentityCommandArguments>
{
    private readonly GitCommitIdentityCommandService _service;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public ManageCommitIdentityCommandResolver(
        GitCommitIdentityCommandService service,
        KnownGitRepositorysRepository knownRepositories)
    {
        _service = service;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<ManageCommitIdentityCommandArguments> ArgumentsJsonTypeInfo =>
        ConfigurationJsonSerializerContext.Default.ManageCommitIdentityCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ManageCommitIdentity;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ManageCommitIdentityCommandArguments> command)
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

            var identity = command.Arguments.ClearRepositoryOverride
                ? await _service.ClearAsync(
                    repository.Path,
                    CancellationToken.None).ConfigureAwait(false)
                : await _service.SaveAsync(
                    repository.Path,
                    command.Arguments.Name,
                    command.Arguments.Email,
                    CancellationToken.None).ConfigureAwait(false);
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
        NativeCommand<ManageCommitIdentityCommandArguments> command,
        string message) => new()
    {
        CommandUniqueId = command.CommandUniqueId,
        CommandType = command.CommandType,
        IsSuccess = false,
        ErrorMessage = message,
    };
}
