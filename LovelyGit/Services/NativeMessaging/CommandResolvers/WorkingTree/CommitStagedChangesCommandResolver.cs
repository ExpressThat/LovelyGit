using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class CommitStagedChangesCommandResolver : CommandResponder<CommitStagedChangesCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly WorkingTreeIndexService _workingTreeIndexService;

    protected override JsonTypeInfo<CommitStagedChangesCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.CommitStagedChangesCommandArguments;

    public CommitStagedChangesCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        WorkingTreeIndexService workingTreeIndexService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _workingTreeIndexService = workingTreeIndexService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.CommitStagedChanges;
    }

    public override async Task<CommandResponseBase> Resolve(NativeCommand<CommitStagedChangesCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(command.Arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _workingTreeIndexService
                .CommitStagedChangesAsync(
                    foundRepo.Path,
                    command.Arguments.Title,
                    command.Arguments.Body,
                    command.Arguments.Amend,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(NativeCommand<CommitStagedChangesCommandArguments> command)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };
    }

    private static CommandResponseBase Failure(
        NativeCommand<CommitStagedChangesCommandArguments> command,
        string errorMessage)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
    }
}
