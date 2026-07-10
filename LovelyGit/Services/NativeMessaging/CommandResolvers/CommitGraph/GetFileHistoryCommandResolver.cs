using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.FileHistory;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class GetFileHistoryCommandResolver : CommandResponder<GetFileHistoryCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly FileHistoryService _historyService;

    public GetFileHistoryCommandResolver(
        KnownGitRepositorysRepository repositories,
        FileHistoryService historyService)
    {
        _repositories = repositories;
        _historyService = historyService;
    }

    protected override JsonTypeInfo<GetFileHistoryCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetFileHistoryCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetFileHistory;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetFileHistoryCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.KnownRepositoryId == Guid.Empty)
        {
            return Failure(command, "KnownRepositoryId is required.");
        }

        if (string.IsNullOrWhiteSpace(arguments.Path))
        {
            return Failure(command, "A repository-relative file path is required.");
        }

        var repository = await _repositories.FindByIdAsync(arguments.KnownRepositoryId)
            .ConfigureAwait(false);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var response = await _historyService.ReadAsync(
                repository.Id,
                repository.Path,
                arguments.Path,
                arguments.StartCommitHash,
                arguments.Limit,
                arguments.Deep).ConfigureAwait(false);
            return new CommandResponse<FileHistoryResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = response,
            };
        }
        catch (OperationCanceledException)
        {
            return Failure(command, "File history was superseded by a newer request.");
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetFileHistoryCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
