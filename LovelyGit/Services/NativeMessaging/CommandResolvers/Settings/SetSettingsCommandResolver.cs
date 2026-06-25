using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Settings
{
    public class SetSettingsCommandResolver : CommandResponder<SetSettingsCommandArguments>
    {
        private readonly SettingsManager _settingsManager;

        public SetSettingsCommandResolver(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected override JsonTypeInfo<SetSettingsCommandArguments> ArgumentsJsonTypeInfo =>
            SettingsJsonSerializerContext.Default.SetSettingsCommandArguments;

        public override bool CanRespondTo(NativeCommand<JsonElement> command)
        {
            return command.CommandType == NativeMessageType.SetSetting;
        }

        public override async Task<CommandResponseBase> Resolve(NativeCommand<SetSettingsCommandArguments> command)
        {
            if (command.Arguments?.Setting == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = "Missing setting argument",
                };
            }

            if (command.Arguments.ValueJson == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = "Missing setting value",
                };
            }

            if (!SettingsResolver.TryGetDefinition(command.Arguments.Setting.Value, out var setting))
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = $"Unknown setting: {command.Arguments.Setting}",
                };
            }

            await _settingsManager.SetSettingValue(setting, command.Arguments.ValueJson);

            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
            };
        }
    }
}
