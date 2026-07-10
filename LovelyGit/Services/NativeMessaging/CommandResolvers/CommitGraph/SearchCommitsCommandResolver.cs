using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class SearchCommitsCommandResolver : CommandResponder<SearchCommitsCommandArguments>
{
    private const int MinimumQueryLength = 2;
    private readonly KnownGitRepositorysRepository _repositories;
    private readonly CommitSearchService _searchService;

    public SearchCommitsCommandResolver(
        KnownGitRepositorysRepository repositories,
        CommitSearchService searchService)
    {
        _repositories = repositories;
        _searchService = searchService;
    }

    protected override JsonTypeInfo<SearchCommitsCommandArguments> ArgumentsJsonTypeInfo =>
        CommitGraphJsonSerializerContext.Default.SearchCommitsCommandArguments;

    public override bool CanRespondTo(NativeCommand<JsonElement> command) =>
        command.CommandType == NativeMessageType.SearchCommits;

    public override async Task<CommandResponseBase> Resolve(
        NativeCommand<SearchCommitsCommandArguments> command)
    {
        var arguments = command.Arguments;
        var query = arguments?.Query.Trim() ?? string.Empty;
        if (arguments == null || arguments.KnownRepositoryId == Guid.Empty)
        {
            return Failure(command, "KnownRepositoryId is required.");
        }

        if (query.Length < MinimumQueryLength)
        {
            return Failure(command, "Enter at least two characters to search commits.");
        }

        var repository = await _repositories.FindByIdAsync(arguments.KnownRepositoryId)
            .ConfigureAwait(false);
        if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
        {
            return Failure(command, "Known repository not found.");
        }

        try
        {
            var response = await _searchService.SearchAsync(
                repository.Id,
                repository.Path,
                query,
                arguments.Limit,
                arguments.Deep).ConfigureAwait(false);
            return new CommandResponse<CommitSearchResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = response,
            };
        }
        catch (OperationCanceledException)
        {
            return Failure(command, "Commit search was superseded by a newer query.");
        }
        catch (Exception exception)
        {
            return Failure(command, exception.Message);
        }
    }

    private static CommandResponseBase Failure(
        NativeCommand<SearchCommitsCommandArguments> command,
        string message) =>
        new()
        {
            CommandUniqueId = command.CommandUniqueId,
            CommandType = command.CommandType,
            IsSuccess = false,
            ErrorMessage = message,
        };
}
