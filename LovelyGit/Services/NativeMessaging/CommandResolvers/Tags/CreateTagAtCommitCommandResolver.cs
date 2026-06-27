using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Tags;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

internal sealed class CreateTagAtCommitCommandResolver
    : CommandResponder<CreateTagAtCommitCommandArguments>
{
    private readonly GitTagCommandService _tagCommandService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<CreateTagAtCommitCommandArguments> ArgumentsJsonTypeInfo =>
        TagsJsonSerializerContext.Default.CreateTagAtCommitCommandArguments;

    public CreateTagAtCommitCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitTagCommandService tagCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _tagCommandService = tagCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.CreateTagAtCommit;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CreateTagAtCommitCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (string.IsNullOrWhiteSpace(arguments.CommitHash))
        {
            return Failure(command, "Commit hash is required.");
        }

        if (!GitTagNameValidator.IsValidTagName(arguments.TagName))
        {
            return Failure(command, "Tag name is not valid.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _tagCommandService.CreateTagAsync(
                foundRepo.Path,
                arguments.TagName.Trim(),
                arguments.CommitHash,
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<CreateTagAtCommitCommandArguments> command)
    {
        return new CommandResponse<EmptyCommandArguments>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = new EmptyCommandArguments(),
        };
    }

    private static CommandResponseBase Failure(
        NativeCommand<CreateTagAtCommitCommandArguments> command,
        string errorMessage)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
    }
}
