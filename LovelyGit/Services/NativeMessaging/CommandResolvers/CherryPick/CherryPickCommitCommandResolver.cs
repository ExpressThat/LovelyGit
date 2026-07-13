using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;

internal sealed class CherryPickCommitCommandResolver
    : CommandResponder<CherryPickCommitCommandArguments>
{
    private readonly GitRepositoryOperationService _repositoryOperations;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public CherryPickCommitCommandResolver(
        GitRepositoryOperationService repositoryOperations,
        KnownGitRepositorysRepository knownRepositories)
    {
        _repositoryOperations = repositoryOperations;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<CherryPickCommitCommandArguments> ArgumentsJsonTypeInfo =>
        CherryPickJsonSerializerContext.Default.CherryPickCommitCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CherryPickCommit;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CherryPickCommitCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments is null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var repository = await _knownRepositories.FindByIdAsync(arguments.RepositoryId)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(repository?.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var result = await _repositoryOperations.CherryPickAsync(
                    repository.Path,
                    arguments.CommitHashes,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command, result);
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<CherryPickCommitCommandArguments> command,
        GitRepositoryOperationOutcome result) =>
        new CommandResponse<RepositoryOperationCommandResponse>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = new RepositoryOperationCommandResponse
            {
                IsCompleted = result.IsCompleted,
                Operation = result.Operation,
                Message = result.Message,
            },
        };

    private static CommandResponseBase Failure(
        NativeCommand<CherryPickCommitCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
