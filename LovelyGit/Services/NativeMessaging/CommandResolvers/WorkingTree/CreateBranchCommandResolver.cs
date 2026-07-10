using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class CreateBranchCommandResolver : CommandResponder<CreateBranchCommandArguments>
{
    private readonly GitBranchCommandService _branchCommands;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public CreateBranchCommandResolver(
        GitBranchCommandService branchCommands,
        KnownGitRepositorysRepository knownRepositories)
    {
        _branchCommands = branchCommands;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<CreateBranchCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.CreateBranchCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CreateBranch;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CreateBranchCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository foundRepo = await _knownRepositories.FindByIdAsync(arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _branchCommands.CreateAsync(
                foundRepo.Path,
                arguments.BranchName,
                arguments.StartPoint,
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(NativeCommand<CreateBranchCommandArguments> command) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };

    private static CommandResponseBase Failure(
        NativeCommand<CreateBranchCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
