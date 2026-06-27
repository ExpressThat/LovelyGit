using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CherryPick;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;

internal sealed class CherryPickCommitCommandResolver
    : CommandResponder<CherryPickCommitCommandArguments>
{
    private readonly GitCherryPickCommandService _cherryPickCommandService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<CherryPickCommitCommandArguments> ArgumentsJsonTypeInfo =>
        CherryPickJsonSerializerContext.Default.CherryPickCommitCommandArguments;

    public CherryPickCommitCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitCherryPickCommandService cherryPickCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _cherryPickCommandService = cherryPickCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.CherryPickCommit;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CherryPickCommitCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (!GitObjectId.TryParse(arguments.CommitHash, out _))
        {
            return Failure(command, "Commit hash is not valid.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _cherryPickCommandService.CherryPickCommitAsync(
                foundRepo.Path,
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
        NativeCommand<CherryPickCommitCommandArguments> command)
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
        NativeCommand<CherryPickCommitCommandArguments> command,
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
