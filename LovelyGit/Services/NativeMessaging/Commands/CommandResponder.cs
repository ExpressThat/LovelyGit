using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.Commands
{
    public abstract class CommandResponder<TArguments> : ICommandResponder
    {
        protected abstract JsonTypeInfo<TArguments> ArgumentsJsonTypeInfo { get; }

        public abstract bool CanRespondTo(NativeCommand<JsonElement> command);

        async Task<CommandResponseBase> ICommandResponder.Resolve(NativeCommand<JsonElement> command)
        {
            TArguments? arguments = default;
            if (command.Arguments.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            {
                arguments = command.Arguments.Deserialize(ArgumentsJsonTypeInfo);
            }

            return await Resolve(new NativeCommand<TArguments>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                Arguments = arguments,
            });
        }

        public abstract Task<CommandResponseBase> Resolve(NativeCommand<TArguments> command);
    }
}
