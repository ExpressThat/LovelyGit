using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Tags;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

internal sealed class CreateTagAtCommitCommandResolver
    : CommandResponder<CreateTagAtCommitCommandArguments>
{
    private readonly GitTagCommandService _tags;
    private readonly KnownGitRepositorysRepository _repositories;

    public CreateTagAtCommitCommandResolver(
        GitTagCommandService tags,
        KnownGitRepositorysRepository repositories)
    {
        _tags = tags;
        _repositories = repositories;
    }

    protected override JsonTypeInfo<CreateTagAtCommitCommandArguments> ArgumentsJsonTypeInfo =>
        TagsJsonSerializerContext.Default.CreateTagAtCommitCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CreateTagAtCommit;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CreateTagAtCommitCommandArguments> command)
    {
        var arguments = command.Arguments;
        var repository = arguments is null
            ? null
            : await _repositories.FindByIdAsync(arguments.RepositoryId).ConfigureAwait(false);
        if (arguments is null || string.IsNullOrWhiteSpace(repository?.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _tags.CreateTagAsync(
                repository.Path,
                arguments.TagName,
                arguments.CommitHash,
                arguments.IsAnnotated,
                arguments.Message,
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<CreateTagAtCommitCommandArguments> command) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };

    private static CommandResponseBase Failure(
        NativeCommand<CreateTagAtCommitCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
