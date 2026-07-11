using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class SaveCommitArchiveCommandResolver(
    KnownGitRepositorysRepository knownRepositories,
    CommitArchiveExportService exportService)
    : CommandResponder<GetCommitPatchCommandArguments>
{
    protected override JsonTypeInfo<GetCommitPatchCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetCommitPatchCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.SaveCommitArchive;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetCommitPatchCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (!GitObjectId.TryParse(command.Arguments.CommitHash, out var commitId))
        {
            return Failure(command, "CommitHash is invalid.");
        }

        KnownGitRepository repository = await knownRepositories
            .FindByIdAsync(command.Arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var response = await exportService
                .ExportAsync(repository.Path, commitId, CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command, response);
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<GetCommitPatchCommandArguments> command,
        CommitArchiveExportResponse response) =>
        new CommandResponse<CommitArchiveExportResponse>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = response,
        };

    private static CommandResponseBase Failure(
        NativeCommand<GetCommitPatchCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
