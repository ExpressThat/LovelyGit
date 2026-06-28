using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class DiscardWorkingTreeChangesCommandResolver
    : CommandResponder<DiscardWorkingTreeChangesCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly WorkingTreeIndexService _workingTreeIndexService;

    protected override JsonTypeInfo<DiscardWorkingTreeChangesCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.DiscardWorkingTreeChangesCommandArguments;

    public DiscardWorkingTreeChangesCommandResolver(
        KnownGitRepositorysRepository knownRepositories,
        WorkingTreeIndexService workingTreeIndexService)
    {
        _knownRepositories = knownRepositories;
        _workingTreeIndexService = workingTreeIndexService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.DiscardWorkingTreeChanges;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<DiscardWorkingTreeChangesCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository foundRepo = await _knownRepositories.FindByIdAsync(command.Arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _workingTreeIndexService
                .DiscardChangesAsync(foundRepo.Path, command.Arguments.Files, CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<DiscardWorkingTreeChangesCommandArguments> command) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };

    private static CommandResponseBase Failure(
        NativeCommand<DiscardWorkingTreeChangesCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
