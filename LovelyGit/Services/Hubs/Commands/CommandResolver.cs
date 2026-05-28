namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    public class CommandResolver
    {
        private readonly IEnumerable<ICommandResponder> _commandResponders;

        public CommandResolver(IEnumerable<ICommandResponder> commandResolvers)
        {
            _commandResponders = commandResolvers;
        }

        public async Task<CommandResponseBase> ResolveCommand(CommsHubCommand command)
        {
            var responder = _commandResponders.FirstOrDefault(r => r.CanRespondTo(command));
            if (responder == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    SubCommandType = command.SubCommandType,
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
                    SubCommandType = command.SubCommandType,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                };
            }
        }
    }
}
