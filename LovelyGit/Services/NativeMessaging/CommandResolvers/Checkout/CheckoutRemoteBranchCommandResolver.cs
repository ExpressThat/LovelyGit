using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Checkout;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;

internal sealed class CheckoutRemoteBranchCommandResolver
    : CommandResponder<CheckoutRemoteBranchCommandArguments>
{
    private readonly GitCheckoutCommandService _checkoutCommandService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<CheckoutRemoteBranchCommandArguments> ArgumentsJsonTypeInfo =>
        CheckoutJsonSerializerContext.Default.CheckoutRemoteBranchCommandArguments;

    public CheckoutRemoteBranchCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitCheckoutCommandService checkoutCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _checkoutCommandService = checkoutCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command)
    {
        return command.CommandType == NativeMessageType.CheckoutRemoteBranch;
    }

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CheckoutRemoteBranchCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        if (!GitBranchNameValidator.IsValidBranchName(arguments.RemoteBranchName)
            || !GitBranchNameValidator.IsValidBranchName(arguments.LocalBranchName))
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
            await _checkoutCommandService.CheckoutRemoteBranchAsync(
                foundRepo.Path,
                arguments.RemoteBranchName.Trim(),
                arguments.LocalBranchName.Trim(),
                CancellationToken.None).ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(NativeCommand<CheckoutRemoteBranchCommandArguments> command)
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
        NativeCommand<CheckoutRemoteBranchCommandArguments> command,
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
