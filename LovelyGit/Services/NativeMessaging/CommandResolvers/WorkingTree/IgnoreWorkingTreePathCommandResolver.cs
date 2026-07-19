using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class IgnoreWorkingTreePathCommandResolver
    : CommandResponder<IgnoreWorkingTreePathCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly GitIgnoreService _service;
    private readonly WorkingTreeStatusListService _statusService;

    public IgnoreWorkingTreePathCommandResolver(
        KnownGitRepositorysRepository knownRepositories,
        GitIgnoreService service,
        WorkingTreeStatusListService statusService)
    {
        _knownRepositories = knownRepositories;
        _service = service;
        _statusService = statusService;
    }

    protected override JsonTypeInfo<IgnoreWorkingTreePathCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.IgnoreWorkingTreePathCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.IgnoreWorkingTreePath;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<IgnoreWorkingTreePathCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository repository = await _knownRepositories
            .FindByIdAsync(command.Arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var result = await _service.AddExactPathAsync(
                    repository.Path,
                    command.Arguments.Path,
                    command.Arguments.Target,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (result.Target == GitIgnoreTarget.Shared)
            {
                result.TargetChanges = await TryReadSharedIgnoreStatusAsync(
                        repository.Path,
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
            return new CommandResponse<GitIgnoreResult>
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

    private async Task<WorkingTreeChangesResponse?> TryReadSharedIgnoreStatusAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _statusService
                .GetChangesForPathAsync(repositoryPath, ".gitignore", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<IgnoreWorkingTreePathCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
