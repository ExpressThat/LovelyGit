using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal sealed class SearchCommitsCommandResolver : CommandResponder<SearchCommitsCommandArguments>
{
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
        if (arguments is not { KnownRepositoryId: var repositoryId }
            || repositoryId == Guid.Empty)
        {
            return Failure(command, "KnownRepositoryId is required.");
        }

        var author = arguments.Author.Trim();
        var validationError = CommitSearchRequestValidator.Validate(arguments);
        if (validationError != null)
        {
            return Failure(command, validationError);
        }

        var repository = await _repositories.FindByIdAsync(repositoryId)
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
                author,
                arguments.AfterUnixSeconds,
                arguments.BeforeUnixSeconds,
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
