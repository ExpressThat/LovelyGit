using ExpressThat.LovelyGit.Services.Hubs.Commands;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace ExpressThat.LovelyGit.Services.Hubs
{
    public class CommsHub : Hub
    {
        private CommandResolver _resolver;
        public CommsHub(CommandResolver resolver) : base()
        {
            _resolver = resolver;
        }

        public async Task Command(CommsHubCommand<JsonElement> command)
        {
            var result = await _resolver.ResolveCommand(command);
            await Clients.All.SendAsync("Result", result);
        }
    }
}
