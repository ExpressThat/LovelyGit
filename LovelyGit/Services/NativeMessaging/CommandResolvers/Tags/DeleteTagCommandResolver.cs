using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Tags;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

internal sealed class DeleteTagCommandResolver : CommandResponder<DeleteTagCommandArguments>
{
    private readonly GitTagCommandService _tags;
    private readonly KnownGitRepositorysRepository _repositories;

    public DeleteTagCommandResolver(
        GitTagCommandService tags,
        KnownGitRepositorysRepository repositories)
    {
        _tags = tags;
        _repositories = repositories;
    }

    protected override JsonTypeInfo<DeleteTagCommandArguments> ArgumentsJsonTypeInfo =>
        TagsJsonSerializerContext.Default.DeleteTagCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.DeleteTag;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<DeleteTagCommandArguments> command)
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
            await _tags.DeleteTagAsync(
                repository.Path,
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
        NativeCommand<DeleteTagCommandArguments> command,
        bool isSuccess,
        string? message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = isSuccess,
            ErrorMessage = message,
        };
}
