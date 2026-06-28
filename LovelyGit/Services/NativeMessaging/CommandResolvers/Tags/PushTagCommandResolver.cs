using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Tags;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

internal sealed class PushTagCommandResolver : CommandResponder<PushTagCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
    private readonly GitTagCommandService _tagCommandService;

    protected override JsonTypeInfo<PushTagCommandArguments> ArgumentsJsonTypeInfo =>
        TagsJsonSerializerContext.Default.PushTagCommandArguments;

    public PushTagCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitTagCommandService tagCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _tagCommandService = tagCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.PushTag;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<PushTagCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (!GitRemoteNameValidator.IsValidRemoteName(arguments.RemoteName))
        {
            return Failure(command, "Remote name is not valid.");
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
                .PushTagAsync(
                    foundRepo.Path,
                    arguments.RemoteName.Trim(),
                    arguments.TagName.Trim(),
                    CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(NativeCommand<PushTagCommandArguments> command) =>
        new CommandResponse<EmptyCommandArguments>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = new EmptyCommandArguments(),
        };

    private static CommandResponseBase Failure(
        NativeCommand<PushTagCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
