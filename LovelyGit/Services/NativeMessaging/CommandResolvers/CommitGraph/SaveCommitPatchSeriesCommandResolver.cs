using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class SaveCommitPatchSeriesCommandResolver
    : CommandResponder<CommitPatchSeriesCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly CommitPatchSeriesExportService _service;

    protected override JsonTypeInfo<CommitPatchSeriesCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.CommitPatchSeriesCommandArguments;

    public SaveCommitPatchSeriesCommandResolver(
        KnownGitRepositorysRepository repositories,
        CommitPatchSeriesExportService service)
    {
        _repositories = repositories;
        _service = service;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.SaveCommitPatchSeries;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CommitPatchSeriesCommandArguments> command)
    {
        if (!CommitPatchSeriesArguments.TryParse(command.Arguments, out var ids, out var error))
        {
            return Failure(command, error);
        }

        var repository = await _repositories.FindByIdAsync(command.Arguments!.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var result = await _service.ExportAsync(repository.Path, ids, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<CommitPatchExportResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = result,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<CommitPatchSeriesCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            ErrorMessage = message,
        };
}
