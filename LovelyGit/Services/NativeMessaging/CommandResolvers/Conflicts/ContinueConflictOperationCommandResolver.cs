using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.Conflicts;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Conflicts;

internal sealed class ContinueConflictOperationCommandResolver
    : CommandResponder<CompleteConflictOperationCommandArguments>
{
    private readonly GitConflictCommandService _commandService;
    private readonly ConflictRepositoryResolver _repositoryResolver;

    protected override JsonTypeInfo<CompleteConflictOperationCommandArguments> ArgumentsJsonTypeInfo =>
        ConflictJsonSerializerContext.Default.CompleteConflictOperationCommandArguments;

    public ContinueConflictOperationCommandResolver(
        GitConflictCommandService commandService,
        ConflictRepositoryResolver repositoryResolver)
    {
        _commandService = commandService;
        _repositoryResolver = repositoryResolver;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ContinueConflictOperation;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CompleteConflictOperationCommandArguments> command)
    {
        var path = await _repositoryResolver.ResolvePathAsync(command.Arguments?.RepositoryId ?? Guid.Empty);
        if (path == null)
        {
            return ConflictRepositoryResolver.Failure(
                command.CommandUniqueId,
                command.CommandType,
                "Known repository not found.");
        }

        try
        {
            await _commandService.ContinueAsync(path, CancellationToken.None);
            return ConflictRepositoryResolver.EmptySuccess(command.CommandUniqueId, command.CommandType);
        }
        catch (Exception exception)
        {
            return ConflictRepositoryResolver.Failure(
                command.CommandUniqueId,
                command.CommandType,
                exception.Message);
        }
    }
}
