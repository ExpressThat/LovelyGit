using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Merge;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;

internal sealed class MergeBranchIntoCurrentCommandResolver
    : CommandResponder<MergeBranchIntoCurrentCommandArguments>
{
    private readonly GitMergeCommandService _mergeCommandService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<MergeBranchIntoCurrentCommandArguments> ArgumentsJsonTypeInfo =>
        MergeJsonSerializerContext.Default.MergeBranchIntoCurrentCommandArguments;

    public MergeBranchIntoCurrentCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitMergeCommandService mergeCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _mergeCommandService = mergeCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.MergeBranchIntoCurrent;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<MergeBranchIntoCurrentCommandArguments> command)
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
            await _mergeCommandService.MergeBranchIntoCurrentAsync(
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

    private static CommandResponseBase Success(NativeCommand<MergeBranchIntoCurrentCommandArguments> command)
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
        NativeCommand<MergeBranchIntoCurrentCommandArguments> command,
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
