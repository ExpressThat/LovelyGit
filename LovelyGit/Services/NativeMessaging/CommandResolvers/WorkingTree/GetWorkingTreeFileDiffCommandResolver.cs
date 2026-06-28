using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class GetWorkingTreeFileDiffCommandResolver : CommandResponder<GetWorkingTreeFileDiffArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly WorkingTreeChangeService _workingTreeChangeService;

    protected override JsonTypeInfo<GetWorkingTreeFileDiffArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.GetWorkingTreeFileDiffArguments;

    public GetWorkingTreeFileDiffCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        WorkingTreeChangeService workingTreeChangeService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _workingTreeChangeService = workingTreeChangeService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.GetWorkingTreeFileDiff;
    }

    public override async Task<CommandResponseBase> Resolve(NativeCommand<GetWorkingTreeFileDiffArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Arguments.Path))
        {
            return Failure(command, "Path is required.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(command.Arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var response = await _workingTreeChangeService
                .GetFileDiffAsync(
                    foundRepo.Path,
                    command.Arguments.Path,
                    command.Arguments.Group,
                    command.Arguments.ViewMode,
                    command.Arguments.IgnoreWhitespace,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<CommitFileDiffResponse>
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
        NativeCommand<GetWorkingTreeFileDiffArguments> command,
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
