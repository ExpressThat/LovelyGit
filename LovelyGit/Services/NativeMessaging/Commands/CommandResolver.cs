using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.Commands
{
    internal sealed class CommandResolver
    {
        private readonly CommandResponderCatalog _catalog;
        private readonly IServiceProvider _services;

        public CommandResolver(
            CommandResponderCatalog catalog,
            IServiceProvider services)
        {
            _catalog = catalog;
            _services = services;
        }

        public async Task<CommandResponseBase> ResolveCommand(NativeCommand<JsonElement> command)
        {
            if (!_catalog.TryGetResponderType(command.CommandType, out var responderType))
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = $"No responder found for command type: {command.CommandType}",
                };
            }
            var responder = (ICommandResponder)_services.GetRequiredService(responderType);
            if (!responder.CanRespondTo(command))
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = $"Responder mapping is invalid for command type: {command.CommandType}",
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
