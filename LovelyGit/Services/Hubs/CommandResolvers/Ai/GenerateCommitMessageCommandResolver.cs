using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Ai;
using ExpressThat.LovelyGit.Services.Ai.Models;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Hubs.Commands;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Ai;

internal sealed class GenerateCommitMessageCommandResolver : CommandResponder<GenerateCommitMessageCommandArguments>
{
    private readonly AiCommitMessageService _aiCommitMessageService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    public GenerateCommitMessageCommandResolver(
        AiCommitMessageService aiCommitMessageService,
        KnownGitRepositorysRepository knownGitRepositorysRepository)
    {
        _aiCommitMessageService = aiCommitMessageService;
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
    }

    protected override JsonTypeInfo<GenerateCommitMessageCommandArguments> ArgumentsJsonTypeInfo =>
        AiJsonSerializerContext.Default.GenerateCommitMessageCommandArguments;

    public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
    {
        return command.CommandType == CommsHubCommandType.GenerateCommitMessage;
    }

    public override async Task<CommandResponseBase> Resolve(CommsHubCommand<GenerateCommitMessageCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(command.Arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var response = await _aiCommitMessageService
                .GenerateCommitMessageAsync(foundRepo.Path, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<GenerateCommitMessageResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = response,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        CommsHubCommand<GenerateCommitMessageCommandArguments> command,
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
