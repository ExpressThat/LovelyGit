using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Hubs.Commands;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.WorkingTree;

internal sealed class PushRepositoryCommandResolver : CommandResponder<GitRemoteCommandArguments>
{
    private readonly GitRemoteCommandService _gitRemoteCommandService;
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<GitRemoteCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.GitRemoteCommandArguments;

    public PushRepositoryCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository,
        GitRemoteCommandService gitRemoteCommandService)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
        _gitRemoteCommandService = gitRemoteCommandService;
    }

    public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
    {
        return command.CommandType == CommsHubCommandType.PushRepository;
    }

    public override async Task<CommandResponseBase> Resolve(CommsHubCommand<GitRemoteCommandArguments> command)
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
            await _gitRemoteCommandService.PushAsync(foundRepo.Path, CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(CommsHubCommand<GitRemoteCommandArguments> command)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };
    }

    private static CommandResponseBase Failure(
        CommsHubCommand<GitRemoteCommandArguments> command,
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
