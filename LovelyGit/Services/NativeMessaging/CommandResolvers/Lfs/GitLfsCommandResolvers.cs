using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Lfs;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Lfs;

internal sealed class GetGitLfsStateCommandResolver
    : CommandResponder<GetGitLfsStateCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly NativeGitLfsStateReader _reader;

    public GetGitLfsStateCommandResolver(
        KnownGitRepositorysRepository repositories,
        NativeGitLfsStateReader reader)
    {
        _repositories = repositories;
        _reader = reader;
    }

    protected override JsonTypeInfo<GetGitLfsStateCommandArguments> ArgumentsJsonTypeInfo =>
        LfsJsonSerializerContext.Default.GetGitLfsStateCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetGitLfsState;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetGitLfsStateCommandArguments> command)
    {
        var repository = await FindRepositoryAsync(command.Arguments?.RepositoryId)
            .ConfigureAwait(false);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }
        try
        {
            var state = await _reader.ReadAsync(repository.Path, CancellationToken.None)
                .ConfigureAwait(false);
            return Success(command, state);
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private async Task<KnownGitRepository?> FindRepositoryAsync(Guid? repositoryId)
    {
        if (!repositoryId.HasValue || repositoryId.Value == Guid.Empty) return null;
        return await _repositories.FindByIdAsync(repositoryId.Value).ConfigureAwait(false);
    }

    private static CommandResponse<LfsRepositoryState> Success(
        NativeCommand<GetGitLfsStateCommandArguments> command,
        LfsRepositoryState state) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = state,
        };

    private static CommandResponseBase Failure(
        NativeCommand<GetGitLfsStateCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}

internal sealed class ManageGitLfsCommandResolver
    : CommandResponder<ManageGitLfsCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly GitLfsCommandService _service;
    private readonly NativeGitLfsStateReader _reader;

    public ManageGitLfsCommandResolver(
        KnownGitRepositorysRepository repositories,
        GitLfsCommandService service,
        NativeGitLfsStateReader reader)
    {
        _repositories = repositories;
        _service = service;
        _reader = reader;
    }

    protected override JsonTypeInfo<ManageGitLfsCommandArguments> ArgumentsJsonTypeInfo =>
        LfsJsonSerializerContext.Default.ManageGitLfsCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ManageGitLfs;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ManageGitLfsCommandArguments> command)
    {
        if (command.Arguments == null || command.Arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var repository = await _repositories.FindByIdAsync(command.Arguments.RepositoryId)
            .ConfigureAwait(false);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            await _service.ExecuteAsync(
                    repository.Path,
                    command.Arguments.Action,
                    command.Arguments.Pattern,
                    CancellationToken.None)
                .ConfigureAwait(false);
            var state = await _reader.ReadAsync(repository.Path, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<LfsRepositoryState>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = state,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<ManageGitLfsCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
