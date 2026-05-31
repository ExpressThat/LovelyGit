using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.KnownRepository
{
    [TranspilationSource]
    public record RemoveKnownGitRepositorysCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
    }

    public class RemoveKnownGitRepositorysCommandResolver : CommandResponder<RemoveKnownGitRepositorysCommandArguments>
    {
        private readonly KnownGitRepositorysRepository _knownGitRepositorysRepository;

        protected override JsonTypeInfo<RemoveKnownGitRepositorysCommandArguments> ArgumentsJsonTypeInfo =>
            KnownRepositoriesJsonSerializerContext.Default.RemoveKnownGitRepositorysCommandArguments;

        public RemoveKnownGitRepositorysCommandResolver(KnownGitRepositorysRepository knownGitRepositorysRepository)
        {
            _knownGitRepositorysRepository = knownGitRepositorysRepository;
        }

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.RemoveKnownGitRepositorys;
        }

        public override async Task<CommandResponseBase> Resolve(
            CommsHubCommand<RemoveKnownGitRepositorysCommandArguments> command)
        {
            if (command.Arguments == null || command.Arguments.KnownRepositoryId == Guid.Empty)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = "KnownRepositoryId is required.",
                };
            }

            await _knownGitRepositorysRepository.RemoveAsync(command.Arguments.KnownRepositoryId);

            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
            };
        }
    }
}
