using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class GetWorkingTreeChangesCommandResolver : CommandResponder<GetWorkingTreeChangesCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly WorkingTreeStatusListService _workingTreeStatusListService;

    protected override JsonTypeInfo<GetWorkingTreeChangesCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.GetWorkingTreeChangesCommandArguments;

    public GetWorkingTreeChangesCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        WorkingTreeStatusListService workingTreeStatusListService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _workingTreeStatusListService = workingTreeStatusListService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.GetWorkingTreeChanges;
    }

    public override async Task<CommandResponseBase> Resolve(NativeCommand<GetWorkingTreeChangesCommandArguments> command)
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
            var response = await _workingTreeStatusListService
                .GetChangesAsync(
                    foundRepo.Path,
                    command.Arguments.TrackedOnly,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<WorkingTreeChangesResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = response,
            };
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetWorkingTreeChangesCommandArguments> command,
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
