using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.SparseCheckout;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.SparseCheckout;

internal sealed class GetSparseCheckoutStateCommandResolver(
    KnownGitRepositorysRepository knownRepositories,
    NativeSparseCheckoutReader reader)
    : CommandResponder<GetSparseCheckoutStateCommandArguments>
{
    protected override JsonTypeInfo<GetSparseCheckoutStateCommandArguments> ArgumentsJsonTypeInfo =>
        SparseCheckoutJsonSerializerContext.Default.GetSparseCheckoutStateCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetSparseCheckoutState;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetSparseCheckoutStateCommandArguments> command)
    {
        var repository = await FindRepositoryAsync(knownRepositories, command.Arguments?.RepositoryId)
            .ConfigureAwait(false);
        if (repository == null) return Failure(command, "Known repository not found.");
        try
        {
            var state = await reader.ReadAsync(repository.Path!, CancellationToken.None).ConfigureAwait(false);
            return Success(command, state);
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    internal static async Task<KnownGitRepository?> FindRepositoryAsync(
        KnownGitRepositorysRepository knownRepositories,
        Guid? repositoryId)
    {
        if (!repositoryId.HasValue || repositoryId.Value == Guid.Empty) return null;
        var repository = await knownRepositories.FindByIdAsync(repositoryId.Value);
        return string.IsNullOrWhiteSpace(repository?.Path) ? null : repository;
    }

    internal static CommandResponseBase Success<TArguments>(
        NativeCommand<TArguments> command,
        SparseCheckoutState state) =>
        new CommandResponse<SparseCheckoutState>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = SparseCheckoutPayloadCompactor.CompactIfUseful(state),
        };

    internal static CommandResponseBase Failure<TArguments>(
        NativeCommand<TArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}

internal sealed class ManageSparseCheckoutCommandResolver(
    KnownGitRepositorysRepository knownRepositories,
    GitSparseCheckoutCommandService commandService)
    : CommandResponder<ManageSparseCheckoutCommandArguments>
{
    protected override JsonTypeInfo<ManageSparseCheckoutCommandArguments> ArgumentsJsonTypeInfo =>
        SparseCheckoutJsonSerializerContext.Default.ManageSparseCheckoutCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ManageSparseCheckout;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ManageSparseCheckoutCommandArguments> command)
    {
        var repository = await GetSparseCheckoutStateCommandResolver
            .FindRepositoryAsync(knownRepositories, command.Arguments?.RepositoryId)
            .ConfigureAwait(false);
        if (repository == null || command.Arguments == null)
        {
            return GetSparseCheckoutStateCommandResolver.Failure(command, "Known repository not found.");
        }

        try
        {
            var state = await commandService.ExecuteAsync(
                    repository.Path!,
                    command.Arguments.Action,
                    command.Arguments.ConeMode,
                    SparseCheckoutPayloadCompactor.ExpandRequest(
                        command.Arguments.PatternText,
                        command.Arguments.PatternTextGzipBase64),
                    CancellationToken.None)
                .ConfigureAwait(false);
            return GetSparseCheckoutStateCommandResolver.Success(command, state);
        }
        catch (Exception exception)
        {
            return GetSparseCheckoutStateCommandResolver.Failure(command, exception.Message);
        }
    }
}
