using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class GetHeadCommitMessageCommandResolver
    : CommandResponder<GetHeadCommitMessageCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly HeadCommitMessageService _messages;

    protected override JsonTypeInfo<GetHeadCommitMessageCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.GetHeadCommitMessageCommandArguments;

    public GetHeadCommitMessageCommandResolver(
        KnownGitRepositorysRepository repositories,
        HeadCommitMessageService messages)
    {
        _repositories = repositories;
        _messages = messages;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetHeadCommitMessage;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetHeadCommitMessageCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var repository = await _repositories.FindByIdAsync(command.Arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var result = await _messages.GetAsync(repository.Path, CancellationToken.None).ConfigureAwait(false);
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
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetHeadCommitMessageCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
