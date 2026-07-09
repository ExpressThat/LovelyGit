using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;

internal sealed class GetRepositoryOperationStateCommandResolver
    : CommandResponder<GetRepositoryOperationStateCommandArguments>
{
    private readonly GitBranchIntegrationService _branchIntegration;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public GetRepositoryOperationStateCommandResolver(
        GitBranchIntegrationService branchIntegration,
        KnownGitRepositorysRepository knownRepositories)
    {
        _branchIntegration = branchIntegration;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<GetRepositoryOperationStateCommandArguments> ArgumentsJsonTypeInfo =>
        RepositoryOperationsJsonSerializerContext.Default.GetRepositoryOperationStateCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetRepositoryOperationState;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetRepositoryOperationStateCommandArguments> command)
    {
        var repository = await FindRepositoryAsync(command.Arguments?.RepositoryId ?? Guid.Empty)
            .ConfigureAwait(false);
        if (repository is null)
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var operation = await _branchIntegration
                .GetOperationAsync(repository.Path!, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<RepositoryOperationStateResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = new RepositoryOperationStateResponse { Operation = operation },
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private async Task<KnownGitRepository?> FindRepositoryAsync(Guid repositoryId)
    {
        if (repositoryId == Guid.Empty)
        {
            return null;
        }

        var repository = await _knownRepositories.FindByIdAsync(repositoryId).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(repository?.Path) ? null : repository;
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetRepositoryOperationStateCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}

internal sealed class ContinueRepositoryOperationCommandResolver
    : CommandResponder<RepositoryOperationCommandArguments>
{
    private readonly GitBranchIntegrationService _branchIntegration;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public ContinueRepositoryOperationCommandResolver(
        GitBranchIntegrationService branchIntegration,
        KnownGitRepositorysRepository knownRepositories)
    {
        _branchIntegration = branchIntegration;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<RepositoryOperationCommandArguments> ArgumentsJsonTypeInfo =>
        RepositoryOperationsJsonSerializerContext.Default.RepositoryOperationCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ContinueRepositoryOperation;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<RepositoryOperationCommandArguments> command)
    {
        var repository = await FindRepositoryAsync(command.Arguments?.RepositoryId ?? Guid.Empty)
            .ConfigureAwait(false);
        if (repository is null || command.Arguments is null)
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var result = await _branchIntegration.ContinueAsync(
                    repository.Path!,
                    command.Arguments.Operation,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command, result);
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private async Task<KnownGitRepository?> FindRepositoryAsync(Guid repositoryId)
    {
        if (repositoryId == Guid.Empty)
        {
            return null;
        }

        var repository = await _knownRepositories.FindByIdAsync(repositoryId).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(repository?.Path) ? null : repository;
    }

    private static CommandResponseBase Success(
        NativeCommand<RepositoryOperationCommandArguments> command,
        GitBranchIntegrationOutcome result) =>
        new CommandResponse<BranchIntegrationCommandResponse>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = new BranchIntegrationCommandResponse
            {
                IsCompleted = result.IsCompleted,
                Operation = result.Operation,
                Message = result.Message,
            },
        };

    private static CommandResponseBase Failure(
        NativeCommand<RepositoryOperationCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}

internal sealed class AbortRepositoryOperationCommandResolver
    : CommandResponder<RepositoryOperationCommandArguments>
{
    private readonly GitBranchIntegrationService _branchIntegration;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public AbortRepositoryOperationCommandResolver(
        GitBranchIntegrationService branchIntegration,
        KnownGitRepositorysRepository knownRepositories)
    {
        _branchIntegration = branchIntegration;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<RepositoryOperationCommandArguments> ArgumentsJsonTypeInfo =>
        RepositoryOperationsJsonSerializerContext.Default.RepositoryOperationCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.AbortRepositoryOperation;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<RepositoryOperationCommandArguments> command)
    {
        var repository = await FindRepositoryAsync(command.Arguments?.RepositoryId ?? Guid.Empty)
            .ConfigureAwait(false);
        if (repository is null || command.Arguments is null)
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _branchIntegration.AbortAsync(
                    repository.Path!,
                    command.Arguments.Operation,
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

    private async Task<KnownGitRepository?> FindRepositoryAsync(Guid repositoryId)
    {
        if (repositoryId == Guid.Empty)
        {
            return null;
        }

        var repository = await _knownRepositories.FindByIdAsync(repositoryId).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(repository?.Path) ? null : repository;
    }

    private static CommandResponseBase Failure(
        NativeCommand<RepositoryOperationCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
