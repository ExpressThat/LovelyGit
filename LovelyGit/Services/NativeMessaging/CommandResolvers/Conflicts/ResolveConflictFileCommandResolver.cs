using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.Conflicts;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Conflicts;

internal sealed class ResolveConflictFileCommandResolver
    : CommandResponder<ResolveConflictFileCommandArguments>
{
    private readonly GitConflictCommandService _commandService;
    private readonly ConflictRepositoryResolver _repositoryResolver;

    protected override JsonTypeInfo<ResolveConflictFileCommandArguments> ArgumentsJsonTypeInfo =>
        ConflictJsonSerializerContext.Default.ResolveConflictFileCommandArguments;

    public ResolveConflictFileCommandResolver(
        GitConflictCommandService commandService,
        ConflictRepositoryResolver repositoryResolver)
    {
        _commandService = commandService;
        _repositoryResolver = repositoryResolver;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ResolveConflictFile;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ResolveConflictFileCommandArguments> command)
    {
        var arguments = command.Arguments;
        var path = await _repositoryResolver.ResolvePathAsync(arguments?.RepositoryId ?? Guid.Empty);
        if (path == null || arguments == null)
        {
            return ConflictRepositoryResolver.Failure(
                command.CommandUniqueId,
                command.CommandType,
                "Known repository not found.");
        }

        try
        {
            await _commandService.ResolveFileAsync(
                path,
                arguments.Path,
                arguments.Action,
                arguments.ResultText,
                CancellationToken.None);
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
