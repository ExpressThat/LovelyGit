using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

internal sealed class InitializeRepositoryCommandResolver :
    CommandResponder<InitializeRepositoryCommandArguments>
{
    private readonly GitInitializeService _initializeService;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public InitializeRepositoryCommandResolver(
        GitInitializeService initializeService,
        KnownGitRepositorysRepository knownRepositories)
    {
        _initializeService = initializeService;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<InitializeRepositoryCommandArguments> ArgumentsJsonTypeInfo =>
        KnownRepositoriesJsonSerializerContext.Default.InitializeRepositoryCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.InitializeRepository;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<InitializeRepositoryCommandArguments> command)
    {
        if (command.Arguments == null)
        {
            return Failure(command, "Repository details are required.");
        }

        try
        {
            var path = await _initializeService.InitializeAsync(
                    command.Arguments.ParentPath,
                    command.Arguments.DirectoryName,
                    command.Arguments.InitialBranchName,
                    CancellationToken.None)
                .ConfigureAwait(false);
            var repository = await _knownRepositories.AddAsync(new KnownGitRepository
            {
                Id = Guid.NewGuid(),
                Name = new DirectoryInfo(path).Name,
                Path = path,
            }).ConfigureAwait(false);
            return new CommandResponse<KnownGitRepository>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = repository,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<InitializeRepositoryCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            ErrorMessage = message,
            IsSuccess = false,
        };
}
