using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class UnstageWorkingTreeLineCommandResolver : CommandResponder<StageWorkingTreeLineCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly WorkingTreeIndexService _workingTreeIndexService;

    protected override JsonTypeInfo<StageWorkingTreeLineCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.StageWorkingTreeLineCommandArguments;

    public UnstageWorkingTreeLineCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        WorkingTreeIndexService workingTreeIndexService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _workingTreeIndexService = workingTreeIndexService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.UnstageWorkingTreeLine;
    }

    public override async Task<CommandResponseBase> Resolve(NativeCommand<StageWorkingTreeLineCommandArguments> command)
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
                .UnstageLineAsync(
                    foundRepo.Path,
                    command.Arguments.Path,
                    command.Arguments.ChangeType,
                    command.Arguments.OldLineNumber,
                    command.Arguments.NewLineNumber,
                    command.Arguments.OldText ?? string.Empty,
                    command.Arguments.NewText ?? string.Empty,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(NativeCommand<StageWorkingTreeLineCommandArguments> command)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };
    }

    private static CommandResponseBase Failure(
        NativeCommand<StageWorkingTreeLineCommandArguments> command,
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
