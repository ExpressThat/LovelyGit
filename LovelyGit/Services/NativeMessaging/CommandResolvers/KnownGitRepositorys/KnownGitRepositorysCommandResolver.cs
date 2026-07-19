using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository
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

        public override bool CanRespondTo(NativeCommand<JsonElement> command)
        {
            return command.CommandType == NativeMessageType.KnownGitRepositorys;
        }

        public override async Task<CommandResponseBase> Resolve(NativeCommand<EmptyCommandArguments> command)
        {
            var knownGitRepositorys = await _knownGitRepositorysRepository.GetAllAsync();

            return new CommandResponse<KnownGitRepositoriesResponse>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = KnownRepositoriesPayloadCompactor.Compact(knownGitRepositorys)
            };
        }
    }
}
