using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Platform;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

[TypeSharp]
public sealed record OpenRemoteWebResourceCommandArguments
{
    public Guid KnownRepositoryId { get; set; }

    public RemoteWebResourceKind Kind { get; set; }

    public string? Value { get; set; }

    public string? TargetValue { get; set; }
}

internal sealed class OpenRemoteWebResourceCommandResolver
    : CommandResponder<OpenRemoteWebResourceCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly RemoteWebLauncher _launcher;

    protected override JsonTypeInfo<OpenRemoteWebResourceCommandArguments> ArgumentsJsonTypeInfo =>
        KnownRepositoriesJsonSerializerContext.Default.OpenRemoteWebResourceCommandArguments;

    public OpenRemoteWebResourceCommandResolver(
        KnownGitRepositorysRepository repositories,
        RemoteWebLauncher launcher)
    {
        _repositories = repositories;
        _launcher = launcher;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.OpenRemoteWebResource;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<OpenRemoteWebResourceCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.KnownRepositoryId == Guid.Empty)
        {
            return Failure(command, "KnownRepositoryId is required.");
        }

        try
        {
            var repository = await _repositories.FindByIdAsync(arguments.KnownRepositoryId);
            if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
            {
                return Failure(command, "Known repository not found.");
            }

            var gitDirectory = await GitRepositoryDiscovery.ResolveGitDirectoryAsync(
                repository.Path,
                CancellationToken.None).ConfigureAwait(false);
            var remoteUrl = await GitRemoteConfigReader.ReadPrimaryRemoteUrlAsync(
                gitDirectory,
                CancellationToken.None).ConfigureAwait(false);
            if (remoteUrl == null)
            {
                return Failure(command, "Add a remote before opening this repository on the web.");
            }

            _launcher.Open(RemoteWebUrlBuilder.Build(
                remoteUrl,
                arguments.Kind,
                arguments.Value,
                arguments.TargetValue));
            return Success(command);
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<OpenRemoteWebResourceCommandArguments> command) =>
        new CommandResponse<EmptyCommandArguments>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = new EmptyCommandArguments(),
        };

    private static CommandResponseBase Failure(
        NativeCommand<OpenRemoteWebResourceCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
