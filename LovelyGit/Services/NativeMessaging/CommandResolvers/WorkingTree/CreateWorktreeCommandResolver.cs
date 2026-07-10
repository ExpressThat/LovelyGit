using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Worktrees;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class CreateWorktreeCommandResolver : CommandResponder<CreateWorktreeCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly GitWorktreeCommandService _worktrees;

    public CreateWorktreeCommandResolver(
        KnownGitRepositorysRepository repositories,
        GitWorktreeCommandService worktrees)
    {
        _repositories = repositories;
        _worktrees = worktrees;
    }

    protected override JsonTypeInfo<CreateWorktreeCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.CreateWorktreeCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.CreateWorktree;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<CreateWorktreeCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var repository = await _repositories.FindByIdAsync(arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _worktrees.CreateAsync(
                repository.Path,
                arguments.WorktreePath,
                arguments.BranchName,
                CancellationToken.None).ConfigureAwait(false);
            return Respond(command, true);
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<CreateWorktreeCommandArguments> command,
        string message) => Respond(command, false, message);

    private static CommandResponseBase Respond(
        NativeCommand<CreateWorktreeCommandArguments> command,
        bool success,
        string? message = null) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = success,
            ErrorMessage = message,
        };
}
