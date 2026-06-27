using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

internal sealed class DeleteBranchCommandResolver
    : CommandResponder<DeleteBranchCommandArguments>
{
    private readonly GitBranchCommandService _branchCommandService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<DeleteBranchCommandArguments> ArgumentsJsonTypeInfo =>
        BranchesJsonSerializerContext.Default.DeleteBranchCommandArguments;

    public DeleteBranchCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitBranchCommandService branchCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _branchCommandService = branchCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.DeleteBranch;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<DeleteBranchCommandArguments> command)
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
            await _branchCommandService.DeleteBranchAsync(
                foundRepo.Path,
                arguments.BranchName.Trim(),
                arguments.Force,
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(NativeCommand<DeleteBranchCommandArguments> command)
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
        NativeCommand<DeleteBranchCommandArguments> command,
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
