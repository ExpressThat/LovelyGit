using ExpressThat.LovelyGit;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph
{
    internal class CommitGraphCommandResolver : CommandResponder<CommitGraphCommandArguments>
    {
        private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;
        private readonly CommitGraphRepository _commitGraphRepository;
        private readonly CommitDetailsPreloadService _commitDetailsPreloadService;
        private readonly Dictionary<Guid, CommitGraphManager> _activeGraphs = new();

        protected override JsonTypeInfo<CommitGraphCommandArguments> ArgumentsJsonTypeInfo =>
            CommitGraphJsonSerializerContext.Default.CommitGraphCommandArguments;

        public CommitGraphCommandResolver(
            KnownGitRepositorysRepository knownGitRepositorysRepository,
            CommitGraphRepository commitGraphRepository,
            CommitDetailsPreloadService commitDetailsPreloadService)
        {
            _knownGitRepositorysRepository = knownGitRepositorysRepository;
            _commitGraphRepository = commitGraphRepository;
            _commitDetailsPreloadService = commitDetailsPreloadService;
        }

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.CommitGraph;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<CommitGraphCommandArguments> command)
        {
            if (command.Arguments?.KnownRepositoryId == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = "KnownRepositoryId is required."
                };
            }

            KnownGitRepository foundRepo = await _knownGitRepositorysRepository.FindByIdAsync(command.Arguments.KnownRepositoryId);

            if (foundRepo == null || string.IsNullOrWhiteSpace(foundRepo.Path))
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = "Known repository not found."
                };
            }

            if (command.Arguments == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = "Invalid commit graph arguments",
                };
            }

            var limit = command.Arguments.Limit;
            var cursorText = command.Arguments.Cursor;

            if (limit < 0)
            {
                limit = 0;
            }

            var isFreshGraphLoad = string.IsNullOrWhiteSpace(cursorText);
            if (isFreshGraphLoad)
            {
                await _commitDetailsPreloadService.CancelActiveAsync(CancellationToken.None);
                await _commitGraphRepository.ClearRepositoryAsync(foundRepo.Id);
            }

            var cursorState = CommitGraphManager.DecodeCursorState(cursorText);
            try
            {
                if (!_activeGraphs.TryGetValue(foundRepo.Id, out var graph))
                {
                    var openResult = await CommitGraphManager.TryOpenAsync(
                        foundRepo.Path,
                        foundRepo.Id,
                        _commitGraphRepository);
                    if (!openResult.Success || openResult.Graph == null)
                    {
                        return new CommandResponseBase
                        {
                            CommandUniqueId = command.CommandUniqueId,
                            CommandType = command.CommandType,
                            IsSuccess = false,
                            ErrorMessage = openResult.Error ?? "Failed to open native commit-graph."
                        };

                    }

                    graph = openResult.Graph;
                    _activeGraphs[foundRepo.Id] = graph;
                }

                var page = await graph.GetCommitGraphPageAsync(cursorState, limit);

                var response = page.Response;
                response.NextCursor = response.HasMore ? CommitGraphManager.EncodeCursorState(page.NextCursor) : null;
                await TrySaveCachedCommitsAsync(foundRepo.Id, response).ConfigureAwait(false);
                await _commitDetailsPreloadService
                    .StartOrSwitchAsync(foundRepo.Id, foundRepo.Path, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!response.HasMore)
                {
                    if (_activeGraphs.Remove(foundRepo.Id, out var completedGraph))
                    {
                        completedGraph.Dispose();
                    }
                }

                return new CommandResponse<CommitGraphResponse>
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = true,
                    Result = response
                };
            }
            catch (Exception ex)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task TrySaveCachedCommitsAsync(Guid repositoryId, CommitGraphResponse response)
        {
            try
            {
                await _commitGraphRepository
                    .SaveCachedCommitsAsync(repositoryId, response, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }
}
