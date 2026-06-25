using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.Commands
{

    public interface ICommandResponder
    {
        bool CanRespondTo(NativeCommand<JsonElement> command);

        Task<CommandResponseBase> Resolve(NativeCommand<JsonElement> command);
    }
}
