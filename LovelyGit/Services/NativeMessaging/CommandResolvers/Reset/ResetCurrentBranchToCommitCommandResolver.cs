using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.Reset;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;

internal sealed class ResetCurrentBranchToCommitCommandResolver
    : CommandResponder<ResetCurrentBranchToCommitCommandArguments>
{
    private readonly GitResetCommandService _resetCommandService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<ResetCurrentBranchToCommitCommandArguments> ArgumentsJsonTypeInfo =>
        ResetJsonSerializerContext.Default.ResetCurrentBranchToCommitCommandArguments;

    public ResetCurrentBranchToCommitCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitResetCommandService resetCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _resetCommandService = resetCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.ResetCurrentBranchToCommit;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ResetCurrentBranchToCommitCommandArguments> command)
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
            await _resetCommandService.ResetCurrentBranchToCommitAsync(
                foundRepo.Path,
                arguments.CommitHash,
                arguments.ResetMode,
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<ResetCurrentBranchToCommitCommandArguments> command)
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
        NativeCommand<ResetCurrentBranchToCommitCommandArguments> command,
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
