using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class CheckoutBranchCommandResolver : CommandResponder<CheckoutBranchCommandArguments>
{
    private readonly GitBranchCommandService _branchCommands;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public CheckoutBranchCommandResolver(
        GitBranchCommandService branchCommands,
        KnownGitRepositorysRepository knownRepositories)
    {
        _branchCommands = branchCommands;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<CheckoutBranchCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.CheckoutBranchCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CheckoutBranch;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CheckoutBranchCommandArguments> command)
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
            await _branchCommands.CheckoutAsync(
                foundRepo.Path,
                arguments.BranchName,
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(NativeCommand<CheckoutBranchCommandArguments> command) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };

    private static CommandResponseBase Failure(
        NativeCommand<CheckoutBranchCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
