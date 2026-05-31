using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings
{
    public class GetAllSettingsCommandResolver : CommandResponder<EmptyCommandArguments>
    {
        private readonly SettingsManager _settingsManager;

        public GetAllSettingsCommandResolver(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected override JsonTypeInfo<EmptyCommandArguments> ArgumentsJsonTypeInfo =>
            CommandJsonSerializerContext.Default.EmptyCommandArguments;

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.GetAllSettings;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<EmptyCommandArguments> command)
        {
            var definitions = SettingsResolver.GetAllDefinitions();
            var settings = new Dictionary<Setting, JsonElement>();

            foreach (var setting in definitions)
            {
                var value = await _settingsManager.GetSettingValue(setting.Value);
                settings.Add(setting.Key, value);
            }

            return new CommandResponse<Dictionary<Setting, JsonElement>>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = settings,
            };
        }
    }
}
