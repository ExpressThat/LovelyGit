using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class UndoLastCommitCommandResolver
    : CommandResponder<UndoLastCommitCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly UndoLastCommitService _undo;

    protected override JsonTypeInfo<UndoLastCommitCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.UndoLastCommitCommandArguments;

    public UndoLastCommitCommandResolver(
        KnownGitRepositorysRepository repositories,
        UndoLastCommitService undo)
    {
        _repositories = repositories;
        _undo = undo;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.UndoLastCommit;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<UndoLastCommitCommandArguments> command)
    {
        try
        {
            var arguments = command.Arguments ??
                throw new InvalidOperationException("Arguments are required.");
            if (arguments.RepositoryId == Guid.Empty)
            {
                throw new InvalidOperationException("RepositoryId is required.");
            }

            var repository = await _repositories.FindByIdAsync(arguments.RepositoryId)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(repository?.Path))
            {
                throw new InvalidOperationException("Known repository not found.");
            }

            var result = await _undo.UndoAsync(
                    repository.Path,
                    arguments.ExpectedHeadHash,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<HeadCommitMessageResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = result,
            };
        }
        catch (Exception exception)
        {
            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                ErrorMessage = exception.Message,
                IsSuccess = false,
            };
        }
    }
}
