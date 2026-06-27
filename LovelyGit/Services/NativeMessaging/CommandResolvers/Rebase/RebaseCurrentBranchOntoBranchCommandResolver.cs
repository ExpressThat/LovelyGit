using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Rebase;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

internal sealed class RebaseCurrentBranchOntoBranchCommandResolver
    : CommandResponder<RebaseCurrentBranchOntoBranchCommandArguments>
{
    private readonly GitRebaseCommandService _rebaseCommandService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<RebaseCurrentBranchOntoBranchCommandArguments> ArgumentsJsonTypeInfo =>
        RebaseJsonSerializerContext.Default.RebaseCurrentBranchOntoBranchCommandArguments;

    public RebaseCurrentBranchOntoBranchCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitRebaseCommandService rebaseCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _rebaseCommandService = rebaseCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.RebaseCurrentBranchOntoBranch;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<RebaseCurrentBranchOntoBranchCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (!GitBranchNameValidator.IsValidBranchName(arguments.BranchName))
        {
            return Failure(command, "Branch name is not valid.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _rebaseCommandService.RebaseCurrentBranchOntoBranchAsync(
                foundRepo.Path,
                arguments.BranchName.Trim(),
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<RebaseCurrentBranchOntoBranchCommandArguments> command)
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
        NativeCommand<RebaseCurrentBranchOntoBranchCommandArguments> command,
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
