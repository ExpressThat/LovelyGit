using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Patches;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class ApplyPatchCommandResolver : CommandResponder<ApplyPatchCommandArguments>
{
    private readonly KnownGitRepositorysRepository _knownRepositories;
    private readonly PatchApplyService _applyService;

    public ApplyPatchCommandResolver(
        KnownGitRepositorysRepository knownRepositories,
        PatchApplyService applyService)
    {
        _knownRepositories = knownRepositories;
        _applyService = applyService;
    }

    protected override JsonTypeInfo<ApplyPatchCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.ApplyPatchCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ApplyPatch;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<ApplyPatchCommandArguments> command)
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
            await _applyService.ApplyAsync(
                    repository.Path,
                    command.Arguments.PatchPath,
                    command.Arguments.StageChanges,
                    command.Arguments.Reverse,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
            };
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<ApplyPatchCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
