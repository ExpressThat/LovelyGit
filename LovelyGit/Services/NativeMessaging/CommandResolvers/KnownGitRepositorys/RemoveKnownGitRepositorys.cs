using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository
{
    [TypeSharp]
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

        public override bool CanRespondTo(NativeCommand<JsonElement> command)
        {
            return command.CommandType == NativeMessageType.RemoveKnownGitRepositorys;
        }

        public override async Task<CommandResponseBase> Resolve(
            NativeCommand<RemoveKnownGitRepositorysCommandArguments> command)
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
