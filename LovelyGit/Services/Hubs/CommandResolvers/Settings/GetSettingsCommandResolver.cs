using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Settings
{
    public class GetSettingsCommandResolver : CommandResponder<GetSettingsCommandArguments>
    {
        private readonly SettingsManager _settingsManager;

        public GetSettingsCommandResolver(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected override JsonTypeInfo<GetSettingsCommandArguments> ArgumentsJsonTypeInfo =>
            SettingsJsonSerializerContext.Default.GetSettingsCommandArguments;

        public override bool CanRespondTo(NativeCommand<JsonElement> command)
        {
            return command.CommandType == NativeMessageType.GetSetting;
        }

        public override async Task<CommandResponseBase> Resolve(NativeCommand<GetSettingsCommandArguments> command)
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

            return new CommandResponse<JsonElement>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                IsSuccess = true,
                Result = await _settingsManager.GetSettingValue(setting),
            };
        }
    }
}
