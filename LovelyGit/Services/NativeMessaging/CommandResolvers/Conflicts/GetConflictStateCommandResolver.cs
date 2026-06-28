using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.Conflicts;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Conflicts;

internal sealed class GetConflictStateCommandResolver
    : CommandResponder<GetConflictStateCommandArguments>
{
    private readonly GitConflictService _conflictService;
    private readonly ConflictRepositoryResolver _repositoryResolver;

    protected override JsonTypeInfo<GetConflictStateCommandArguments> ArgumentsJsonTypeInfo =>
        ConflictJsonSerializerContext.Default.GetConflictStateCommandArguments;

    public GetConflictStateCommandResolver(
        GitConflictService conflictService,
        ConflictRepositoryResolver repositoryResolver)
    {
        _conflictService = conflictService;
        _repositoryResolver = repositoryResolver;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetConflictState;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetConflictStateCommandArguments> command)
    {
        var path = await _repositoryResolver.ResolvePathAsync(command.Arguments?.RepositoryId ?? Guid.Empty);
        if (path == null)
        {
            return ConflictRepositoryResolver.Failure(
                command.CommandUniqueId,
                command.CommandType,
                "Known repository not found.");
        }

        var state = await _conflictService.GetStateAsync(path, CancellationToken.None);
        return new CommandResponse<GitConflictStateResponse>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = state,
        };
    }
}
