using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class SaveCommitPatchCommandResolver
    : CommandResponder<GetCommitPatchCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly CommitPatchExportService _exportService;

    protected override JsonTypeInfo<GetCommitPatchCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetCommitPatchCommandArguments;

    public SaveCommitPatchCommandResolver(
        KnownGitRepositorysRepository knownRepositories,
        CommitPatchExportService exportService)
    {
        _knownRepositories = knownRepositories;
        _exportService = exportService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.SaveCommitPatch;

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

        KnownGitRepository repository = await _knownRepositories
            .FindByIdAsync(command.Arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var response = await _exportService
                .ExportAsync(repository.Path, commitId, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<CommitPatchExportResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = response,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

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
