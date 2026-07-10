using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Reset;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;

internal sealed class ResetCurrentBranchToCommitCommandResolver
    : CommandResponder<ResetCurrentBranchToCommitCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly GitResetCommandService _resetCommands;

    public ResetCurrentBranchToCommitCommandResolver(
        KnownGitRepositorysRepository knownRepositories,
        GitResetCommandService resetCommands)
    {
        _knownRepositories = knownRepositories;
        _resetCommands = resetCommands;
    }

    protected override JsonTypeInfo<ResetCurrentBranchToCommitCommandArguments> ArgumentsJsonTypeInfo =>
        ResetJsonSerializerContext.Default.ResetCurrentBranchToCommitCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ResetCurrentBranchToCommit;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ResetCurrentBranchToCommitCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments is null || arguments.RepositoryId == Guid.Empty)
        {
            return Respond(command, false, "RepositoryId is required.");
        }

        var repository = await _knownRepositories
            .FindByIdAsync(arguments.RepositoryId)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(repository?.Path))
        {
            return Respond(command, false, "Known repository not found.");
        }

        try
        {
            await _resetCommands.ResetCurrentBranchToCommitAsync(
                repository.Path,
                arguments.CommitHash,
                arguments.ResetMode,
                CancellationToken.None).ConfigureAwait(false);
            return Respond(command, true);
        }
        catch (Exception exception)
        {
            return Respond(command, false, exception.Message);
        }
    }

    private static CommandResponseBase Respond(
        NativeCommand<ResetCurrentBranchToCommitCommandArguments> command,
        bool isSuccess,
        string? errorMessage = null) => new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
        };
}
