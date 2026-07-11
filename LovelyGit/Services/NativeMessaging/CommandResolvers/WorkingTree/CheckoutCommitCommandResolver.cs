using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class CheckoutCommitCommandResolver
    : CommandResponder<CheckoutCommitCommandArguments>
{
    private readonly GitBranchCommandService _branches;
    private readonly KnownGitRepositorysRepository _repositories;

    public CheckoutCommitCommandResolver(
        GitBranchCommandService branches,
        KnownGitRepositorysRepository repositories)
    {
        _branches = branches;
        _repositories = repositories;
    }

    protected override JsonTypeInfo<CheckoutCommitCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.CheckoutCommitCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CheckoutCommit;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CheckoutCommitCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Respond(command, false, "RepositoryId is required.");
        }
        var repository = await _repositories.FindByIdAsync(arguments.RepositoryId)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(repository?.Path))
        {
            return Respond(command, false, "Known repository not found.");
        }
        try
        {
            await _branches.CheckoutCommitAsync(
                repository.Path,
                arguments.CommitHash,
                CancellationToken.None).ConfigureAwait(false);
            return Respond(command, true, null);
        }
        catch (Exception exception)
        {
            return Respond(command, false, exception.Message);
        }
    }

    private static CommandResponseBase Respond(
        NativeCommand<CheckoutCommitCommandArguments> command,
        bool success,
        string? error) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = success,
            ErrorMessage = error,
        };
}
