using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;

internal sealed class MergeBranchIntoCurrentCommandResolver
    : CommandResponder<MergeBranchIntoCurrentCommandArguments>
{
    private readonly GitBranchIntegrationService _branchIntegration;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public MergeBranchIntoCurrentCommandResolver(
        GitBranchIntegrationService branchIntegration,
        KnownGitRepositorysRepository knownRepositories)
    {
        _branchIntegration = branchIntegration;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<MergeBranchIntoCurrentCommandArguments> ArgumentsJsonTypeInfo =>
        MergeJsonSerializerContext.Default.MergeBranchIntoCurrentCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.MergeBranchIntoCurrent;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<MergeBranchIntoCurrentCommandArguments> command)
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
            var result = await _branchIntegration.MergeAsync(
                    repository.Path,
                    arguments.BranchName,
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
        NativeCommand<MergeBranchIntoCurrentCommandArguments> command,
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
        NativeCommand<MergeBranchIntoCurrentCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
