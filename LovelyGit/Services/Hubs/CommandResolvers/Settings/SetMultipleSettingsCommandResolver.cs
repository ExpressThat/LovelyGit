using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings
{
    public class SetMultipleSettingsCommandResolver : CommandResponder<SetMultipleSettingsCommandArguments>
    {
        private readonly SettingsManager _settingsManager;

        public SetMultipleSettingsCommandResolver(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected override JsonTypeInfo<SetMultipleSettingsCommandArguments> ArgumentsJsonTypeInfo =>
            SettingsJsonSerializerContext.Default.SetMultipleSettingsCommandArguments;

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.SetMultipleSettings;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<SetMultipleSettingsCommandArguments> command)
        {
            if (command.Arguments == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    IsSuccess = false,
                    ErrorMessage = "Missing settings arguments",
                };
            }

            var settings = new Dictionary<ISettingDefinition, string>();

            foreach (var settingValue in command.Arguments.SettingValueJsons)
            {
                if (!SettingsResolver.TryGetDefinition(settingValue.Key, out var setting))
                {
                    return new CommandResponseBase
                    {
                        CommandUniqueId = command.CommandUniqueId,
                        CommandType = command.CommandType,
                        IsSuccess = false,
                        ErrorMessage = $"Unknown setting: {settingValue.Key}",
                    };
                }

                settings.Add(setting, settingValue.Value);
            }

            foreach (var setting in settings)
            {
                await _settingsManager.SetSettingValue(setting.Key, setting.Value);
            }

            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
            };
        }
    }
}
