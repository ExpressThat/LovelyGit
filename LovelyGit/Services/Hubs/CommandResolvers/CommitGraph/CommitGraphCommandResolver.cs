using ExpressThat.LazyGit;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Hubs.Commands;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph
{
    public class CommitGraphCommandResolver : ICommandResponder
    {
        private KnownGitRepositorysRepository _knownGitRepositorysRepository;
        private readonly Dictionary<Guid, CommitGraphManager> _activeGraphs = new();

        public CommitGraphCommandResolver(KnownGitRepositorysRepository knownGitRepositorysRepository)
        {
            _knownGitRepositorysRepository = knownGitRepositorysRepository;
        }

        public bool CanRespondTo(CommsHubCommand command)
        {
            return command.CommandType == CommsHubCommandType.CommitGraph;
        }

        public async Task<CommandResponseBase> Resolve(CommsHubCommand command)
        {

            var dotGitPath = @"C:\Projects\linux";

            KnownGitRepository? foundRepo = null;

            foreach (KnownGitRepository repo in await _knownGitRepositorysRepository.GetAllAsync())
            {
                if (repo.Path == dotGitPath)
                {
                    foundRepo = repo;
                }
            }

            if (foundRepo == null)
            {
                foundRepo = await _knownGitRepositorysRepository.AddAsync(new KnownGitRepository
                {
                    Id = Guid.NewGuid(),
                    Name = "Local Git Repository",
                    Path = dotGitPath,
                });
            }

            var limit = 0;

            var limitText = command.Arguments?.GetValueOrDefault("limit");
            var cursorText = command.Arguments?.GetValueOrDefault("cursor");

            if (string.IsNullOrWhiteSpace(limitText) || !int.TryParse(limitText, out limit))
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    SubCommandType = command.SubCommandType,
                    IsSuccess = false,
                    ErrorMessage = "Invalid limit argument",
                };
            }

            if (limit < 0)
            {
                limit = 0;
            }

            if (string.IsNullOrWhiteSpace(cursorText))
            {
                GitRepoCacheDbContext.ClearCache();
                if (_activeGraphs.Remove(foundRepo.Id, out var oldGraph))
                {
                    oldGraph.Dispose();
                }
            }

            var cursorState = CommitGraphManager.DecodeCursorState(cursorText);

            using (GitRepoCacheDbContext cache = new GitRepoCacheDbContext())
            {

                CommitGraphRepository cacheRepository = new CommitGraphRepository(cache);

                try
                {
                    if (!_activeGraphs.TryGetValue(foundRepo.Id, out var graph))
                    {
                        var openResult = await CommitGraphManager.TryOpenAsync(
                            dotGitPath,
                            foundRepo.Id,
                            cacheRepository);
                        if (!openResult.Success || openResult.Graph == null)
                        {
                            return new CommandResponseBase
                            {
                                CommandUniqueId = command.CommandUniqueId,
                                CommandType = command.CommandType,
                                SubCommandType = command.SubCommandType,
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
                    if (!response.HasMore && _activeGraphs.Remove(foundRepo.Id, out var completedGraph))
                    {
                        completedGraph.Dispose();
                    }

                    return new CommandResponse<CommitGraphResponse>
                    {
                        CommandUniqueId = command.CommandUniqueId,
                        CommandType = command.CommandType,
                        SubCommandType = command.SubCommandType,
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
                        SubCommandType = command.SubCommandType,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };
                }
            }

        }
    }
}

