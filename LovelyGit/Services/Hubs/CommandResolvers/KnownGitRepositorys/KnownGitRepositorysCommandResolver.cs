using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.KnownRepository
{
    public class KnownGitRepositorysCommandResolver : CommandResponder<EmptyCommandArguments>
    {
        private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

        protected override JsonTypeInfo<EmptyCommandArguments> ArgumentsJsonTypeInfo =>
            CommandJsonSerializerContext.Default.EmptyCommandArguments;

        public KnownGitRepositorysCommandResolver(KnownGitRepositorysRepository knownGitRepositorysRepository)
        {
            _knownGitRepositorysRepository = knownGitRepositorysRepository;
        }

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.KnownGitRepositorys;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<EmptyCommandArguments> command)
        {
            var knownGitRepositorys = await _knownGitRepositorysRepository.GetAllAsync();

            return new CommandResponse<List<KnownGitRepository>>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = knownGitRepositorys
            };
        }
    }
}
