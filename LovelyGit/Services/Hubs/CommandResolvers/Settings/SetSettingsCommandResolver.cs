using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings
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

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.SetSetting;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<SetSettingsCommandArguments> command)
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
