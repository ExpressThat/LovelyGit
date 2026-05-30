using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    public abstract class CommandResponder<TArguments> : ICommandResponder
    {
        protected abstract JsonTypeInfo<TArguments> ArgumentsJsonTypeInfo { get; }

        public abstract bool CanRespondTo(CommsHubCommand<JsonElement> command);

        async Task<CommandResponseBase> ICommandResponder.Resolve(CommsHubCommand<JsonElement> command)
        {
            TArguments? arguments = default;
            if (command.Arguments.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            {
                arguments = command.Arguments.Deserialize(ArgumentsJsonTypeInfo);
            }

            return await Resolve(new CommsHubCommand<TArguments>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                Arguments = arguments,
            });
        }

        public abstract Task<CommandResponseBase> Resolve(CommsHubCommand<TArguments> command);
    }
}
