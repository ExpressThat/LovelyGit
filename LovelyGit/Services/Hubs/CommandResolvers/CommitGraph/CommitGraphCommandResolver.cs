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
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public CommitGraphCommandResolver(KnownGitRepositorysRepository knownGitRepositorysRepository)
        {
            _knownGitRepositorysRepository = knownGitRepositorysRepository;
        }

        public bool CanRespondTo(CommsHubCommand command)
        {
            return command.CommandType == CommsHubCommandType.CommitGraph;
        }

        public async Task<CommandResponse> Resolve(CommsHubCommand command)
        {
            try
            {
                await _semaphore.WaitAsync();
                var dotGitPath = @"C:\Projects\git";

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

                if (string.IsNullOrWhiteSpace(command.Arguments?["limit"]) || !int.TryParse(command.Arguments?["limit"], out int limit))
                {
                    return new CommandResponse
                    {
                        CommandUniqueId = command.CommandUniqueId,
                        CommandType = command.CommandType,
                        SubCommandType = command.SubCommandType,
                        IsSuccess = false,
                        ErrorMessage = "Invalid limit argument",
                    };
                }

                if (limit < 0) limit = 0;

                if (string.IsNullOrWhiteSpace(command.Arguments?["cursor"]))
                {
                    GitRepoCacheDbContext.ClearCache();
                }

                var cursorState = CommitGraphNative.DecodeCursorState(command.Arguments?["cursor"]);

                using (GitRepoCacheDbContext cache = new GitRepoCacheDbContext())
                {

                    CommitGraphRepository cacheRepository = new CommitGraphRepository(cache);

                    try
                    {
                        var openResult = await CommitGraphNative.TryOpenAsync(
                            dotGitPath,
                            foundRepo.Id,
                            cacheRepository);
                        if (!openResult.Success || openResult.Graph == null)
                        {
                            return new CommandResponse
                            {
                                CommandUniqueId = command.CommandUniqueId,
                                CommandType = command.CommandType,
                                SubCommandType = command.SubCommandType,
                                IsSuccess = false,
                                ErrorMessage = openResult.Error ?? "Failed to open native commit-graph."
                            };

                        }

                        using var graph = openResult.Graph;
                        var page = await graph.GetCommitGraphPageAsync(cursorState, limit);
                        var response = page.Response;
                        response.NextCursor = response.HasMore ? CommitGraphNative.EncodeCursorState(page.NextCursor) : null;
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
                        return new CommandResponse
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
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

