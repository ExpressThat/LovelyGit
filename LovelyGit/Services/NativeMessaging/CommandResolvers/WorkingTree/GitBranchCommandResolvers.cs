using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Checkout;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal abstract class GitBranchCommandResolver<TArguments> : CommandResponder<TArguments>
    where TArguments : class
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected GitBranchCommandResolver(KnownGitRepositorysRepository knownGitRepositorysRepository)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
    }

    public override async Task<CommandResponseBase> Resolve(NativeCommand<TArguments> command)
    {
        if (command.Arguments == null || GetRepositoryId(command.Arguments) == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository
            .FindByIdAsync(GetRepositoryId(command.Arguments));
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await RunAsync(foundRepo.Path, command.Arguments, CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    protected abstract Guid GetRepositoryId(TArguments arguments);

    protected abstract Task RunAsync(
        string repositoryPath,
        TArguments arguments,
        CancellationToken cancellationToken);

    private static CommandResponseBase Success(NativeCommand<TArguments> command) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };

    private static CommandResponseBase Failure(
        NativeCommand<TArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}

internal sealed class CheckoutBranchCommandResolver
    : GitBranchCommandResolver<CheckoutBranchCommandArguments>
{
    private readonly GitCheckoutCommandService _checkoutCommandService;

    protected override JsonTypeInfo<CheckoutBranchCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.CheckoutBranchCommandArguments;

    public CheckoutBranchCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitCheckoutCommandService checkoutCommandService)
        : base(knownGitRepositorysRepository)
    {
        _checkoutCommandService = checkoutCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CheckoutBranch;

    protected override Guid GetRepositoryId(CheckoutBranchCommandArguments arguments) =>
        arguments.RepositoryId;

    protected override Task RunAsync(
        string repositoryPath,
        CheckoutBranchCommandArguments arguments,
        CancellationToken cancellationToken)
    {
        return arguments.IsRemote
            ? _checkoutCommandService.CheckoutRemoteBranchAsync(
                repositoryPath,
                arguments.BranchName,
                arguments.LocalBranchName ?? DeriveLocalBranchName(arguments.BranchName),
                cancellationToken)
            : _checkoutCommandService.CheckoutBranchAsync(
                repositoryPath,
                arguments.BranchName,
                cancellationToken);
    }

    private static string DeriveLocalBranchName(string remoteBranchName)
    {
        var slashIndex = remoteBranchName.IndexOf('/', StringComparison.Ordinal);
        return slashIndex >= 0 && slashIndex < remoteBranchName.Length - 1
            ? remoteBranchName[(slashIndex + 1)..]
            : remoteBranchName;
    }
}

internal sealed class CreateBranchCommandResolver
    : GitBranchCommandResolver<CreateBranchCommandArguments>
{
    private readonly GitBranchCommandService _branchCommandService;
    private readonly GitCheckoutCommandService _checkoutCommandService;

    protected override JsonTypeInfo<CreateBranchCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.CreateBranchCommandArguments;

    public CreateBranchCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitBranchCommandService branchCommandService,
        GitCheckoutCommandService checkoutCommandService)
        : base(knownGitRepositorysRepository)
    {
        _branchCommandService = branchCommandService;
        _checkoutCommandService = checkoutCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CreateBranch;

    protected override Guid GetRepositoryId(CreateBranchCommandArguments arguments) =>
        arguments.RepositoryId;

    protected override async Task RunAsync(
        string repositoryPath,
        CreateBranchCommandArguments arguments,
        CancellationToken cancellationToken)
    {
        await _branchCommandService.CreateBranchAsync(
            repositoryPath,
            arguments.BranchName,
            arguments.StartPoint,
            cancellationToken).ConfigureAwait(false);

        if (arguments.Checkout)
        {
            await _checkoutCommandService.CheckoutBranchAsync(
                repositoryPath,
                arguments.BranchName,
                cancellationToken).ConfigureAwait(false);
        }
    }
}

internal sealed class DeleteBranchCommandResolver
    : GitBranchCommandResolver<DeleteBranchCommandArguments>
{
    private readonly GitBranchCommandService _branchCommandService;

    protected override JsonTypeInfo<DeleteBranchCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.DeleteBranchCommandArguments;

    public DeleteBranchCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitBranchCommandService branchCommandService)
        : base(knownGitRepositorysRepository)
    {
        _branchCommandService = branchCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.DeleteBranch;

    protected override Guid GetRepositoryId(DeleteBranchCommandArguments arguments) =>
        arguments.RepositoryId;

    protected override async Task RunAsync(
        string repositoryPath,
        DeleteBranchCommandArguments arguments,
        CancellationToken cancellationToken)
    {
        var currentBranch = await _branchCommandService
            .GetCurrentBranchNameAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (string.Equals(currentBranch, arguments.BranchName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot delete the current branch.");
        }

        await _branchCommandService.DeleteBranchAsync(
            repositoryPath,
            arguments.BranchName,
            arguments.Force,
            cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class RenameBranchCommandResolver
    : GitBranchCommandResolver<RenameBranchCommandArguments>
{
    private readonly GitBranchCommandService _branchCommandService;

    protected override JsonTypeInfo<RenameBranchCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.RenameBranchCommandArguments;

    public RenameBranchCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitBranchCommandService branchCommandService)
        : base(knownGitRepositorysRepository)
    {
        _branchCommandService = branchCommandService;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.RenameBranch;

    protected override Guid GetRepositoryId(RenameBranchCommandArguments arguments) =>
        arguments.RepositoryId;

    protected override Task RunAsync(
        string repositoryPath,
        RenameBranchCommandArguments arguments,
        CancellationToken cancellationToken)
    {
        return _branchCommandService.RenameBranchAsync(
            repositoryPath,
            arguments.BranchName,
            arguments.NewBranchName,
            cancellationToken);
    }
}
