using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Tags;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

internal sealed class DeleteRemoteTagCommandResolver
    : CommandResponder<DeleteRemoteTagCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly GitTagCommandService _tags;

    public DeleteRemoteTagCommandResolver(
        GitTagCommandService tags,
        KnownGitRepositorysRepository repositories)
    {
        _tags = tags;
        _repositories = repositories;
    }

    protected override JsonTypeInfo<DeleteRemoteTagCommandArguments> ArgumentsJsonTypeInfo =>
        TagsJsonSerializerContext.Default.DeleteRemoteTagCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.DeleteRemoteTag;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<DeleteRemoteTagCommandArguments> command)
    {
        var arguments = command.Arguments;
        var repository = arguments is null
            ? null
            : await _repositories.FindByIdAsync(arguments.RepositoryId).ConfigureAwait(false);
        if (arguments is null || string.IsNullOrWhiteSpace(repository?.Path))
        {
            return Respond(command, false, "Known repository not found.");
        }

        try
        {
            await _tags.DeleteRemoteTagAsync(
                repository.Path,
                arguments.RemoteName,
                arguments.TagName,
                CancellationToken.None).ConfigureAwait(false);
            return Respond(command, true, null);
        }
        catch (Exception exception)
        {
            return Respond(command, false, exception.Message);
        }
    }

    private static CommandResponseBase Respond(
        NativeCommand<DeleteRemoteTagCommandArguments> command,
        bool isSuccess,
        string? message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = isSuccess,
            ErrorMessage = message,
        };
}
