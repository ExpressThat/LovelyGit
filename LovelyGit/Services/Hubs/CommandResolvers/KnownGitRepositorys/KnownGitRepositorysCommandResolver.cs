using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Hubs.Commands;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.KnownRepository
{
    public class KnownGitRepositorysCommandResolver : ICommandResponder
    {
        private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

        public KnownGitRepositorysCommandResolver(KnownGitRepositorysRepository knownGitRepositorysRepository)
        {
            _knownGitRepositorysRepository = knownGitRepositorysRepository;
        }

        public bool CanRespondTo(CommsHubCommand command)
        {
            return command.CommandType == CommsHubCommandType.KnownGitRepositorys;
        }

        public async Task<CommandResponse> Resolve(CommsHubCommand command)
        {
            var knownGitRepositorys = await _knownGitRepositorysRepository.GetAllAsync();

            return new CommandResponse<List<KnownGitRepository>>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                SubCommandType = command.SubCommandType,
                IsSuccess = true,
                Result = await knownGitRepositorys.ToListAsync()
            };
        }
    }
}
