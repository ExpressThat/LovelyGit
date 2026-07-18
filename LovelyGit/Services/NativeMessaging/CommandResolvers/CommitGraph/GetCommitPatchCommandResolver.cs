using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class GetCommitPatchCommandResolver : CommandResponder<GetCommitPatchCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly CommitPatchService _commitPatchService;

    protected override JsonTypeInfo<GetCommitPatchCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetCommitPatchCommandArguments;

    public GetCommitPatchCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        CommitPatchService commitPatchService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _commitPatchService = commitPatchService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.GetCommitPatch;
    }

    public override async Task<CommandResponseBase> Resolve(NativeCommand<GetCommitPatchCommandArguments> command)
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
            var response = await _commitPatchService
                .GetCommitPatchAsync(foundRepo.Path, commitId, CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command, CommitPatchPayloadCompactor.Compact(response));
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponse<CommitPatchResponse> Success(
        NativeCommand<GetCommitPatchCommandArguments> command,
        CommitPatchResponse response)
    {
        return new CommandResponse<CommitPatchResponse>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = response,
        };
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetCommitPatchCommandArguments> command,
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
