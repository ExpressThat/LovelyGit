using System.Text.Json;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{

    public interface ICommandResponder
    {
        bool CanRespondTo(CommsHubCommand<JsonElement> command);

        Task<CommandResponseBase> Resolve(CommsHubCommand<JsonElement> command);
    }
}
