using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

internal class GetCommitFileDiffCommandResolver : CommandResponder<GetCommitFileDiffCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly CommitFileDiffService _commitFileDiffService;

    protected override JsonTypeInfo<GetCommitFileDiffCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetCommitFileDiffCommandArguments;

    public GetCommitFileDiffCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        CommitFileDiffService commitFileDiffService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _commitFileDiffService = commitFileDiffService;
    }

    public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
    {
        return command.CommandType == CommsHubCommandType.GetCommitFileDiff;
    }

    public override async Task<CommandResponseBase> Resolve(CommsHubCommand<GetCommitFileDiffCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (!GitObjectId.TryParse(command.Arguments.CommitHash, out _))
        {
            return Failure(command, "CommitHash is invalid.");
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
            var response = await _commitFileDiffService
                .GetCommitFileDiffAsync(
                    foundRepo.Id,
                    foundRepo.Path,
                    command.Arguments.CommitHash,
                    command.Arguments.Path,
                    command.Arguments.ViewMode,
                    CancellationToken.None)
                .ConfigureAwait(false);

            return Success(command, response);
        }
        catch (Exception ex)
        {
            Trace.TraceError("GetCommitFileDiff failed: {0}", ex);
            Console.Error.WriteLine("GetCommitFileDiff failed: {0}", ex);
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponse<CommitFileDiffResponse> Success(
        CommsHubCommand<GetCommitFileDiffCommandArguments> command,
        CommitFileDiffResponse response)
    {
        return new CommandResponse<CommitFileDiffResponse>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = response,
        };
    }

    private static CommandResponseBase Failure(
        CommsHubCommand<GetCommitFileDiffCommandArguments> command,
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
