using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Remotes;

internal sealed class GetRepositoryRemotesCommandResolver
    : CommandResponder<GetRepositoryRemotesCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

    protected override JsonTypeInfo<GetRepositoryRemotesCommandArguments> ArgumentsJsonTypeInfo =>
        RemotesJsonSerializerContext.Default.GetRepositoryRemotesCommandArguments;

    public GetRepositoryRemotesCommandResolver(
        KnownGitRepositorysRepository knownGitRepositorysRepository)
    {
        _knownGitRepositorysRepository = knownGitRepositorysRepository;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetRepositoryRemotes;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetRepositoryRemotesCommandArguments> command)
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
            var paths = await GitRepositoryDiscovery
                .ResolveRepositoryPathsAsync(foundRepo.Path, CancellationToken.None)
                .ConfigureAwait(false);
            var remotes = await GitRemoteConfigReader
                .ReadRemotesAsync(paths.GitDirectory, CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command, remotes);
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<GetRepositoryRemotesCommandArguments> command,
        List<GitRemote> remotes) =>
        new CommandResponse<List<GitRemote>>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = remotes,
        };

    private static CommandResponseBase Failure(
        NativeCommand<GetRepositoryRemotesCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
