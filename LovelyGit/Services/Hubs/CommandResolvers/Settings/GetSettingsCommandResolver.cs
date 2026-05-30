using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings
{
    public class GetSettingsCommandResolver : CommandResponder<GetSettingsCommandArguments>
    {
        private readonly SettingsManager _settingsManager;

        public GetSettingsCommandResolver(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected override JsonTypeInfo<GetSettingsCommandArguments> ArgumentsJsonTypeInfo =>
            CommandReponseJsonSerializerContext.Default.GetSettingsCommandArguments;

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.GetSetting;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<GetSettingsCommandArguments> command)
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

    public class SetSettingsCommandResolver : CommandResponder<SetSettingsCommandArguments>
    {
        private readonly SettingsManager _settingsManager;

        public SetSettingsCommandResolver(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected override JsonTypeInfo<SetSettingsCommandArguments> ArgumentsJsonTypeInfo =>
            CommandReponseJsonSerializerContext.Default.SetSettingsCommandArguments;

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

    public class GetAllSettingsCommandResolver : CommandResponder<EmptyCommandArguments>
    {
        private readonly SettingsManager _settingsManager;

        public GetAllSettingsCommandResolver(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected override JsonTypeInfo<EmptyCommandArguments> ArgumentsJsonTypeInfo =>
            CommandReponseJsonSerializerContext.Default.EmptyCommandArguments;

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

    public class SetMultipleSettingsCommandResolver : CommandResponder<SetMultipleSettingsCommandArguments>
    {
        private readonly SettingsManager _settingsManager;

        public SetMultipleSettingsCommandResolver(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected override JsonTypeInfo<SetMultipleSettingsCommandArguments> ArgumentsJsonTypeInfo =>
            CommandReponseJsonSerializerContext.Default.SetMultipleSettingsCommandArguments;

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
