using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Stashes;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal sealed class StashCommandResolver : CommandResponder<StashCommandArguments>
{
    private readonly GitStashCommandService _stashCommands;
    private readonly KnownGitRepositorysRepository _knownRepositories;

    public StashCommandResolver(
        GitStashCommandService stashCommands,
        KnownGitRepositorysRepository knownRepositories)
    {
        _stashCommands = stashCommands;
        _knownRepositories = knownRepositories;
    }

    protected override JsonTypeInfo<StashCommandArguments> ArgumentsJsonTypeInfo =>
        WorkingTreeJsonSerializerContext.Default.StashCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.ManageStash;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<StashCommandArguments> command)
    {
        var arguments = command.Arguments;
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            return Failure(command, "RepositoryId is required.");
        }

        KnownGitRepository foundRepo = await _knownRepositories.FindByIdAsync(arguments.RepositoryId);
        if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            if (arguments.Action == StashAction.Branch)
            {
                await _stashCommands.BranchFromStashAsync(
                    foundRepo.Path,
                    arguments.Selector,
                    arguments.BranchName,
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _stashCommands.ExecuteAsync(
                    foundRepo.Path,
                    arguments.Action,
                    arguments.Selector,
                    arguments.Message,
                    arguments.IncludeUntracked,
                    arguments.RestoreIndex,
                    arguments.SelectedOnly,
                    arguments.Paths,
                    CancellationToken.None).ConfigureAwait(false);
            }
            return Success(command, await TryReadStashesAsync(foundRepo.Path).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            return Failure(command, ex.Message);
        }
    }

    private static CommandResponseBase Success(
        NativeCommand<StashCommandArguments> command,
        List<RepositoryStashItem>? stashes) =>
        new CommandResponse<StashCommandResponse>
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = true,
            Result = new StashCommandResponse { Stashes = stashes },
        };

    internal static async Task<List<RepositoryStashItem>?> TryReadStashesAsync(string repositoryPath)
    {
        try
        {
            var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
                repositoryPath, CancellationToken.None).ConfigureAwait(false);
            var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(
                paths.GitDirectory, CancellationToken.None).ConfigureAwait(false);
            var stashes = await GitStashReader.ReadAsync(
                paths.GitDirectory, objectFormat, CancellationToken.None).ConfigureAwait(false);
            return stashes.Select(stash => new RepositoryStashItem
            {
                Selector = stash.Selector,
                CommitHash = stash.Target.ToString(),
                Message = stash.Message,
                CreatedAtUnixSeconds = stash.CreatedAtUnixSeconds,
            }).ToList();
        }
        catch
        {
            return null;
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<StashCommandArguments> command,
        string errorMessage) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
}
