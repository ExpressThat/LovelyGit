using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Rebase;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

internal sealed class GetInteractiveRebasePlanCommandResolver
    : CommandResponder<GetInteractiveRebasePlanCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public GetInteractiveRebasePlanCommandResolver(KnownGitRepositorysRepository knownRepositories)
    {
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<GetInteractiveRebasePlanCommandArguments> ArgumentsJsonTypeInfo =>
        RebaseJsonSerializerContext.Default.GetInteractiveRebasePlanCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.GetInteractiveRebasePlan;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<GetInteractiveRebasePlanCommandArguments> command)
    {
        if (command.Arguments is not { RepositoryId: var repositoryId } arguments ||
            repositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var repository = await _knownRepositories.FindByIdAsync(repositoryId).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(repository?.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var result = await NativeInteractiveRebasePlanReader.ReadAsync(
                    repository.Path, arguments.BaseCommitHash, CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponse<InteractiveRebasePlanResponse>
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

    private static CommandResponseBase Failure(
        NativeCommand<GetInteractiveRebasePlanCommandArguments> command,
        string message) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
