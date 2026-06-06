using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

internal sealed class CancelCommitDiffPreparationCommandResolver : CommandResponder<CancelCommitDiffPreparationCommandArguments>
{
    private readonly CommitFileDiffService _commitFileDiffService;

    protected override JsonTypeInfo<CancelCommitDiffPreparationCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.CancelCommitDiffPreparationCommandArguments;

    public CancelCommitDiffPreparationCommandResolver(CommitFileDiffService commitFileDiffService)
    {
        _commitFileDiffService = commitFileDiffService;
    }

    public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
    {
        return command.CommandType == CommsHubCommandType.CancelCommitDiffPreparation;
    }

    public override Task<CommandResponseBase> Resolve(CommsHubCommand<CancelCommitDiffPreparationCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Task.FromResult(Failure(command, "RepositoryId is required."));
        }

        if (!GitObjectId.TryParse(command.Arguments.CommitHash, out _))
        {
            return Task.FromResult(Failure(command, "CommitHash is invalid."));
        }

        _commitFileDiffService.CancelPreparingCommitDiffs(
            command.Arguments.RepositoryId,
            command.Arguments.CommitHash);

        return Task.FromResult(Success(command));
    }

    private static CommandResponseBase Success(
        CommsHubCommand<CancelCommitDiffPreparationCommandArguments> command)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };
    }

    private static CommandResponseBase Failure(
        CommsHubCommand<CancelCommitDiffPreparationCommandArguments> command,
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
