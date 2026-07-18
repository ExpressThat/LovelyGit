using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class GetFileBlameCommandResolver : CommandResponder<GetFileBlameCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly FileBlameService _blameService;

    public GetFileBlameCommandResolver(
        KnownGitRepositorysRepository repositories,
        FileBlameService blameService)
    {
        _repositories = repositories;
        _blameService = blameService;
    }

    protected override JsonTypeInfo<GetFileBlameCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetFileBlameCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetFileBlame;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetFileBlameCommandArguments> command)
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
            var response = await _blameService.ReadAsync(
                repository.Id,
                repository.Path,
                arguments.Path,
                arguments.StartCommitHash,
                arguments.Deep).ConfigureAwait(false);
            return new CommandResponse<FileBlameResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = FileBlamePayloadCompactor.CompactIfUseful(response),
            };
        }
        catch (OperationCanceledException)
        {
            return Failure(command, "File blame was superseded by a newer request.");
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetFileBlameCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
