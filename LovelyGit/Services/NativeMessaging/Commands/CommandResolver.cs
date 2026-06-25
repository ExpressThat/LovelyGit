using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.Commands
{
    public class CommandResolver
    {
        private readonly IEnumerable<ICommandResponder> _commandResponders;

        public CommandResolver(IEnumerable<ICommandResponder> commandResolvers)
        {
            _commandResponders = commandResolvers;
        }

        public async Task<CommandResponseBase> ResolveCommand(NativeCommand<JsonElement> command)
        {
            var responder = _commandResponders.FirstOrDefault(r => r.CanRespondTo(command));
            if (responder == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = $"No responder found for command type: {command.CommandType}",
                };
            }
            try
            {
                return await responder.Resolve(command);
            }
            catch (Exception ex)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                };
            }
        }
    }
}
