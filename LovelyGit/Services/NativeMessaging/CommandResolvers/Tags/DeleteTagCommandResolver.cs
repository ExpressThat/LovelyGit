using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Tags;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

internal sealed class DeleteTagCommandResolver
    : CommandResponder<DeleteTagCommandArguments>
{
    private readonly CommitGraphRepository _commitGraphRepository;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly GitTagCommandService _tagCommandService;

    protected override JsonTypeInfo<DeleteTagCommandArguments> ArgumentsJsonTypeInfo =>
        TagsJsonSerializerContext.Default.DeleteTagCommandArguments;

    public DeleteTagCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitTagCommandService tagCommandService,
        CommitGraphRepository commitGraphRepository)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _tagCommandService = tagCommandService;
        _commitGraphRepository = commitGraphRepository;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.DeleteTag;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<DeleteTagCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (!GitTagNameValidator.IsValidTagName(arguments.TagName))
        {
            return Failure(command, "Tag name is not valid.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository
            .FindByIdAsync(arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _tagCommandService
                .DeleteTagAsync(
                    foundRepo.Path,
                    arguments.TagName.Trim(),
                    CancellationToken.None)
                .ConfigureAwait(false);
            await _commitGraphRepository
                .ClearRepositoryAsync(arguments.RepositoryId, CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(NativeCommand<DeleteTagCommandArguments> command)
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
        NativeCommand<DeleteTagCommandArguments> command,
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
