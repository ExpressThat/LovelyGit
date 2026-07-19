using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class GetRemotesCommandResolver : CommandResponder<GetRemotesCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;

    protected override JsonTypeInfo<GetRemotesCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.GetRemotesCommandArguments;

    public GetRemotesCommandResolver(KnownGitRepositorysRepository repositories)
    {
        _repositories = repositories;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetRemotes;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetRemotesCommandArguments> command)
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
            var gitDirectory = await GitRepositoryDiscovery
                .ResolveGitDirectoryAsync(repository.Path, CancellationToken.None)
                .ConfigureAwait(false);
            var remotes = await GitRemoteConfigReader
                .ReadRemotesAsync(gitDirectory, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<GetRemotesResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = RemotePayloadCompactor.CompactIfUseful(remotes),
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<GetRemotesCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
