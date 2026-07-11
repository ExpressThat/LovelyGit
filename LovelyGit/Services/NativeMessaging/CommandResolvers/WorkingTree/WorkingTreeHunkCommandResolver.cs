using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class WorkingTreeHunkCommandResolver
    : CommandResponder<StageWorkingTreeHunkCommandArguments>
{
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly WorkingTreeIndexService _index;

    protected override JsonTypeInfo<StageWorkingTreeHunkCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.StageWorkingTreeHunkCommandArguments;

    public WorkingTreeHunkCommandResolver(
        KnownGitRepositorysRepository repositories,
        WorkingTreeIndexService index)
    {
        _repositories = repositories;
        _index = index;
    }

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType is NativeMessageType.StageWorkingTreeHunk or
            NativeMessageType.UnstageWorkingTreeHunk;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<StageWorkingTreeHunkCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        var repository = await _repositories.FindByIdAsync(arguments.RepositoryId);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            if (command.CommandType == NativeMessageType.StageWorkingTreeHunk)
            {
                await _index.StageHunkAsync(
                    repository.Path,
                    arguments.Path,
                    arguments.Group,
                    arguments.Lines,
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _index.UnstageHunkAsync(
                    repository.Path,
                    arguments.Path,
                    arguments.Lines,
                    CancellationToken.None).ConfigureAwait(false);
            }

            return Success(command);
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<StageWorkingTreeHunkCommandArguments> command) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
        };

    private static CommandResponseBase Failure(
        NativeCommand<StageWorkingTreeHunkCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
