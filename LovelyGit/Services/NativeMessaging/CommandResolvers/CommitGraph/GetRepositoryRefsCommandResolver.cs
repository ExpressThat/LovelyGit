using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class GetRepositoryRefsCommandResolver
    : CommandResponder<GetRepositoryRefsCommandArguments>
{
    private readonly RepositoryRefsService _repositoryRefsService;

    protected override JsonTypeInfo<GetRepositoryRefsCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.GetRepositoryRefsCommandArguments;

    public GetRepositoryRefsCommandResolver(RepositoryRefsService repositoryRefsService)
    {
        _repositoryRefsService = repositoryRefsService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetRepositoryRefs;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetRepositoryRefsCommandArguments> command)
    {
        if (command.Arguments == null)
        {
            return CreateFailureResponse(command, "Invalid repository refs arguments.");
        }

        var result = await _repositoryRefsService
            .GetRefsAsync(command.Arguments.KnownRepositoryId, CancellationToken.None)
            .ConfigureAwait(false);
        return result == null
            ? CreateFailureResponse(command, "Known repository not found.")
            : new CommandResponse<RepositoryRefsResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = RepositoryRefsPayloadCompactor.CompactIfUseful(result),
            };
    }

    private static CommandResponseBase CreateFailureResponse(
        NativeCommand<GetRepositoryRefsCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
