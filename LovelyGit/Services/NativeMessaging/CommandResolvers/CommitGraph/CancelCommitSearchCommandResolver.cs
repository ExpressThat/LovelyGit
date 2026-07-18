using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class CancelCommitSearchCommandResolver
    : CommandResponder<CancelCommitSearchCommandArguments>
{
    private readonly CommitSearchService _searchService;

    public CancelCommitSearchCommandResolver(CommitSearchService searchService)
    {
        _searchService = searchService;
    }

    protected override JsonTypeInfo<CancelCommitSearchCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.CancelCommitSearchCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CancelCommitSearch;

    public override Task<CommandResponseBase> Resolve(
        NativeCommand<CancelCommitSearchCommandArguments> command)
    {
        if (command.Arguments is not { KnownRepositoryId: var repositoryId }
            || repositoryId == Guid.Empty)
        {
            return Task.FromResult<CommandResponseBase>(new()
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = false,
                ErrorMessage = "KnownRepositoryId is required.",
            });
        }

        _searchService.Cancel(repositoryId);
        return Task.FromResult<CommandResponseBase>(new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        });
    }
}
