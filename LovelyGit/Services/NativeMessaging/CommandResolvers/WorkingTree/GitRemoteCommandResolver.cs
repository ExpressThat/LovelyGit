using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal abstract class GitRemoteCommandResolver : CommandResponder<GitRemoteCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected GitRemoteCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
    }

    protected override JsonTypeInfo<GitRemoteCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.GitRemoteCommandArguments;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GitRemoteCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository foundRepo = await _knownGitRepositorysRepository
            .FindByIdAsync(arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await RunAsync(foundRepo.Path, arguments, CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    protected abstract Task RunAsync(
        string repositoryPath,
        GitRemoteCommandArguments arguments,
        CancellationToken cancellationToken);

    protected static CommandResponseBase Success(
        NativeCommand<GitRemoteCommandArguments> command)
    {
        return new CommandResponseBase
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };
    }

    protected static CommandResponseBase Failure(
        NativeCommand<GitRemoteCommandArguments> command,
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
