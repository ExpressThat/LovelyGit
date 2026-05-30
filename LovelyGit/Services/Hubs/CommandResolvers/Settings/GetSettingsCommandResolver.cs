using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings
{
    public class GetSettingsCommandResolver : CommandResponder<GetSettingsCommandArguments>
    {
        private SettingsManager _settingsManager;
        public GetSettingsCommandResolver(SettingsManager settingsManager) {
            _settingsManager = settingsManager;
        }
        
        protected override JsonTypeInfo<GetSettingsCommandArguments> ArgumentsJsonTypeInfo =>
            CommandReponseJsonSerializerContext.Default.GetSettingsCommandArguments;

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.Settings && command.SubCommandType == CommsHubSubCommandType.Get;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<GetSettingsCommandArguments> command)
        {
            if (command.Arguments?.Setting == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    SubCommandType = command.SubCommandType,
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
                    SubCommandType = command.SubCommandType,
                    IsSuccess = false,
                    ErrorMessage = $"Unknown setting: {command.Arguments.Setting}",
                };
            }

            return new CommandResponse<JsonElement>
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                SubCommandType = command.SubCommandType,
                IsSuccess = true,
                Result = await _settingsManager.GetSettingValue(setting),
            };
        }
    }

    public class SetSettingsCommandResolver : CommandResponder<SetSettingsCommandArguments>
    {
        private SettingsManager _settingsManager;
        public SetSettingsCommandResolver(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected override JsonTypeInfo<SetSettingsCommandArguments> ArgumentsJsonTypeInfo =>
            CommandReponseJsonSerializerContext.Default.SetSettingsCommandArguments;

        public override bool CanRespondTo(CommsHubCommand<JsonElement> command)
        {
            return command.CommandType == CommsHubCommandType.Settings && command.SubCommandType == CommsHubSubCommandType.Set;
        }

        public override async Task<CommandResponseBase> Resolve(CommsHubCommand<SetSettingsCommandArguments> command)
        {
            if (command.Arguments?.Setting == null)
            {
                return new CommandResponseBase
                {
                    CommandUniqueId = command.CommandUniqueId,
                    CommandType = command.CommandType,
                    SubCommandType = command.SubCommandType,
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
                    SubCommandType = command.SubCommandType,
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
                    SubCommandType = command.SubCommandType,
                    IsSuccess = false,
                    ErrorMessage = $"Unknown setting: {command.Arguments.Setting}",
                };
            }

            await _settingsManager.SetSettingValue(setting, command.Arguments.ValueJson);

            return new CommandResponseBase
            {
                CommandUniqueId = command.CommandUniqueId,
                CommandType = command.CommandType,
                SubCommandType = command.SubCommandType,
                IsSuccess = true,
            };
        }
    }
}
