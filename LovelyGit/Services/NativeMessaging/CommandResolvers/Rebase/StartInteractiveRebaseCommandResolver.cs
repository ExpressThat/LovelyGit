using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Rebase;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

internal sealed class StartInteractiveRebaseCommandResolver
    : CommandResponder<StartInteractiveRebaseCommandArguments>
{
    private readonly GitInteractiveRebaseService _interactiveRebase;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public StartInteractiveRebaseCommandResolver(
        GitInteractiveRebaseService interactiveRebase,
        KnownGitRepositorysRepository knownRepositories)
    {
        _interactiveRebase = interactiveRebase;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<StartInteractiveRebaseCommandArguments> ArgumentsJsonTypeInfo =>
        RebaseJsonSerializerContext.Default.StartInteractiveRebaseCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.StartInteractiveRebase;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<StartInteractiveRebaseCommandArguments> command)
    {
        if (command.Arguments is not { RepositoryId: var repositoryId } arguments ||
            repositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var repository = await _knownRepositories.FindByIdAsync(repositoryId).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(repository?.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var result = await _interactiveRebase.StartAsync(
                repository.Path, arguments.BaseCommitHash, arguments.Plan, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<RepositoryOperationCommandResponse>
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
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<StartInteractiveRebaseCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
