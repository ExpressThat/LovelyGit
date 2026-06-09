using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Hubs.Commands;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.WorkingTree;

internal sealed class StageWorkingTreeLineCommandResolver : CommandResponder<StageWorkingTreeLineCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly WorkingTreeIndexService _workingTreeIndexService;

    protected override JsonTypeInfo<StageWorkingTreeLineCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.StageWorkingTreeLineCommandArguments;

    public StageWorkingTreeLineCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        WorkingTreeIndexService workingTreeIndexService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _workingTreeIndexService = workingTreeIndexService;
    }

    public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
    {
        return command.CommandType == CommsHubCommandType.StageWorkingTreeLine;
    }

    public override async Task<CommandResponseBase> Resolve(CommsHubCommand<StageWorkingTreeLineCommandArguments> command)
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
                .StageLineAsync(
                    foundRepo.Path,
                    command.Arguments.Path,
                    command.Arguments.Group,
                    command.Arguments.ChangeType,
                    command.Arguments.OldLineNumber,
                    command.Arguments.NewLineNumber,
                    command.Arguments.OldText,
                    command.Arguments.NewText,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(CommsHubCommand<StageWorkingTreeLineCommandArguments> command)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };
    }

    private static CommandResponseBase Failure(
        CommsHubCommand<StageWorkingTreeLineCommandArguments> command,
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
