using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Git.Conflicts;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Conflicts;

internal sealed class GetConflictFileContentCommandResolver
    : CommandResponder<GetConflictFileContentCommandArguments>
{
    private readonly GitConflictFileContentService _contentService;
    private readonly ConflictRepositoryResolver _repositoryResolver;

    protected override JsonTypeInfo<GetConflictFileContentCommandArguments> ArgumentsJsonTypeInfo =>
        ConflictJsonSerializerContext.Default.GetConflictFileContentCommandArguments;

    public GetConflictFileContentCommandResolver(
        GitConflictFileContentService contentService,
        ConflictRepositoryResolver repositoryResolver)
    {
        _contentService = contentService;
        _repositoryResolver = repositoryResolver;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetConflictFileContent;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetConflictFileContentCommandArguments> command)
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

        var content = await _contentService.GetContentAsync(
            path,
            arguments.Path,
            CancellationToken.None);
        return new CommandResponse<GitConflictFileContentResponse>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = content,
        };
    }
}
