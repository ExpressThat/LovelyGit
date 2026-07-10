using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal abstract class ConflictCommandResolver<TArguments> : CommandResponder<TArguments>
    where TArguments : class
{
    private readonly KnownGitRepositorysRepository _repositories;

    protected ConflictCommandResolver(KnownGitRepositorysRepository repositories)
    {
        _repositories = repositories;
    }

    protected async Task<KnownGitRepository> FindRepositoryAsync(Guid repositoryId)
    {
        if (repositoryId == Guid.Empty)
        {
            throw new InvalidOperationException("RepositoryId is required.");
        }

        var repository = await _repositories.FindByIdAsync(repositoryId).ConfigureAwait(false);
        return repository == null || string.IsNullOrWhiteSpace(repository.Path)
            ? throw new InvalidOperationException("Known repository not found.")
            : repository;
    }

    protected static CommandResponseBase Failure(NativeCommand<TArguments> command, Exception exception) => new()
    {
        CommandUniqueId = command.CommandUniqueId,
        CommandType = command.CommandType,
        IsSuccess = false,
        ErrorMessage = exception.Message,
    };
}

internal sealed class GetConflictResolutionCommandResolver
    : ConflictCommandResolver<GetConflictResolutionCommandArguments>
{
    private readonly ConflictResolutionService _service;

    protected override JsonTypeInfo<GetConflictResolutionCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.GetConflictResolutionCommandArguments;

    public GetConflictResolutionCommandResolver(
        KnownGitRepositorysRepository repositories,
        ConflictResolutionService service) : base(repositories)
    {
        _service = service;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetConflictResolution;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetConflictResolutionCommandArguments> command)
    {
        try
        {
            var arguments = command.Arguments ?? throw new InvalidOperationException("Arguments are required.");
            var repository = await FindRepositoryAsync(arguments.RepositoryId).ConfigureAwait(false);
            var result = await _service.ReadAsync(
                repository.Path!,
                arguments.Path,
                arguments.ViewMode,
                arguments.IgnoreWhitespace,
                CancellationToken.None).ConfigureAwait(false);
            return new CommandResponse<ConflictResolutionResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = result,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception);
        }
    }
}

internal sealed class ResolveConflictCommandResolver : ConflictCommandResolver<ResolveConflictCommandArguments>
{
    private readonly ConflictResolutionService _service;

    protected override JsonTypeInfo<ResolveConflictCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.ResolveConflictCommandArguments;

    public ResolveConflictCommandResolver(
        KnownGitRepositorysRepository repositories,
        ConflictResolutionService service) : base(repositories)
    {
        _service = service;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ResolveConflict;

    public override async Task<CommandResponseBase> Resolve(NativeCommand<ResolveConflictCommandArguments> command)
    {
        try
        {
            var arguments = command.Arguments ?? throw new InvalidOperationException("Arguments are required.");
            var repository = await FindRepositoryAsync(arguments.RepositoryId).ConfigureAwait(false);
            await _service.ResolveAsync(
                repository.Path!,
                arguments.Path,
                arguments.ExpectedFingerprint,
                arguments.ResultText,
                arguments.Source,
                arguments.DeleteResult,
                CancellationToken.None).ConfigureAwait(false);
            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception);
        }
    }
}
