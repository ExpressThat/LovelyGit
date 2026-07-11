using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

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

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.GetCommitFileDiff;
    }

    public override async Task<CommandResponseBase> Resolve(NativeCommand<GetCommitFileDiffCommandArguments> command)
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
            var response = string.IsNullOrWhiteSpace(command.Arguments.ComparisonCommitHash)
                ? await _commitFileDiffService.GetCommitFileDiffAsync(
                    foundRepo.Id,
                    foundRepo.Path,
                    command.Arguments.CommitHash,
                    command.Arguments.ParentIndex,
                    command.Arguments.Path,
                    command.Arguments.ViewMode,
                    command.Arguments.IgnoreWhitespace,
                    CancellationToken.None).ConfigureAwait(false)
                : await _commitFileDiffService.GetCommitComparisonFileDiffAsync(
                    foundRepo.Path,
                    command.Arguments.CommitHash,
                    command.Arguments.ComparisonCommitHash,
                    command.Arguments.Path,
                    command.Arguments.ViewMode,
                    command.Arguments.IgnoreWhitespace,
                    CancellationToken.None).ConfigureAwait(false);

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
        NativeCommand<GetCommitFileDiffCommandArguments> command,
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
        NativeCommand<GetCommitFileDiffCommandArguments> command,
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
