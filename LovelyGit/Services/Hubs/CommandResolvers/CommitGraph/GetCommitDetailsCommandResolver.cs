using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

internal class GetCommitDetailsCommandResolver : CommandResponder<GetCommitDetailsCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly CommitDetailsService _commitDetailsService;
    private readonly CommitFileDiffService _commitFileDiffService;
    private readonly CommitGraphBackgroundWorkerOptions _backgroundWorkerOptions;

    protected override JsonTypeInfo<GetCommitDetailsCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetCommitDetailsCommandArguments;

    public GetCommitDetailsCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        CommitDetailsService commitDetailsService,
        CommitFileDiffService commitFileDiffService,
        CommitGraphBackgroundWorkerOptions backgroundWorkerOptions)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _commitDetailsService = commitDetailsService;
        _commitFileDiffService = commitFileDiffService;
        _backgroundWorkerOptions = backgroundWorkerOptions;
    }

    public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
    {
        return command.CommandType == CommsHubCommandType.GetCommitDetails;
    }

    public override async Task<CommandResponseBase> Resolve(CommsHubCommand<GetCommitDetailsCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (!GitObjectId.TryParse(command.Arguments.CommitHash, out var commitId))
        {
            return Failure(command, "CommitHash is invalid.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(command.Arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var response = await _commitDetailsService
                .GetCommitDetailsAsync(foundRepo.Id, foundRepo.Path, commitId, CancellationToken.None)
                .ConfigureAwait(false);
            if (_backgroundWorkerOptions.EnableCommitFileDiffPreparationWorker)
            {
                _commitFileDiffService.StartPreparingCommitDiffs(
                    foundRepo.Id,
                    foundRepo.Path,
                    response.Hash,
                    response.ChangedFiles);
            }

            return Success(command, response);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponse<CommitDetailsResponse> Success(
        CommsHubCommand<GetCommitDetailsCommandArguments> command,
        CommitDetailsResponse response)
    {
        return new CommandResponse<CommitDetailsResponse>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = response
        };
    }

    private static CommandResponseBase Failure(
        CommsHubCommand<GetCommitDetailsCommandArguments> command,
        string errorMessage)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
