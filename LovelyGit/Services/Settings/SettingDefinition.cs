using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Settings;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.Settings
{
    public interface ISettingDefinition
    {
        string Name { get; }
        object? DefaultValue { get; }
        JsonTypeInfo JsonTypeInfo { get; }
    }

    public sealed record SettingDefinition<T>(
        string Name,
        T DefaultValue,
        JsonTypeInfo<T> JsonTypeInfo) : ISettingDefinition
    {
        object? ISettingDefinition.DefaultValue => DefaultValue;
        JsonTypeInfo ISettingDefinition.JsonTypeInfo => JsonTypeInfo;
    }

    public static class SettingsResolver
    {
        public static readonly SettingDefinition<Guid?> CurrentGitRepositoryId = new(
            nameof(Setting.CurrentGitRepositoryId),
            null,
            GetJsonTypeInfo<Guid?>());

        public static readonly SettingDefinition<string> Theme = new(
            nameof(Setting.Theme),
            "System",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> LightTheme = new(
            nameof(Setting.LightTheme),
            "Morning",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> DarkTheme = new(
            nameof(Setting.DarkTheme),
            "Midnight",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> Font = new(
            nameof(Setting.Font),
            "Inter",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> UiFont = new(
            nameof(Setting.UiFont),
            "Inter",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> CodeFont = new(
            nameof(Setting.CodeFont),
            "Consolas",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> LightUiFont = new(
            nameof(Setting.LightUiFont),
            "Inter",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> LightCodeFont = new(
            nameof(Setting.LightCodeFont),
            "Consolas",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> DarkUiFont = new(
            nameof(Setting.DarkUiFont),
            "Inter",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> DarkCodeFont = new(
            nameof(Setting.DarkCodeFont),
            "Consolas",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> LightAccent = new(
            nameof(Setting.LightAccent),
            "",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> LightBackground = new(
            nameof(Setting.LightBackground),
            "",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> LightForeground = new(
            nameof(Setting.LightForeground),
            "",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> DarkAccent = new(
            nameof(Setting.DarkAccent),
            "",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> DarkBackground = new(
            nameof(Setting.DarkBackground),
            "",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<string> DarkForeground = new(
            nameof(Setting.DarkForeground),
            "",
            GetJsonTypeInfo<string>());

        public static readonly SettingDefinition<CommitDiffViewMode> CommitDiffViewMode = new(
            nameof(Setting.CommitDiffViewMode),
            ExpressThat.LovelyGit.Services.Git.CommitGraph.Models.CommitDiffViewMode.SideBySide,
            GetJsonTypeInfo<CommitDiffViewMode>());

        public static readonly SettingDefinition<CommitDiffLineDisplayMode> CommitDiffLineDisplayMode = new(
            nameof(Setting.CommitDiffLineDisplayMode),
            Settings.CommitDiffLineDisplayMode.Changes,
            GetJsonTypeInfo<CommitDiffLineDisplayMode>());

        public static readonly SettingDefinition<int> CommitDiffContextLines = new(
            nameof(Setting.CommitDiffContextLines),
            8,
            GetJsonTypeInfo<int>());

        public static readonly SettingDefinition<bool> CommitDiffWrapLines = new(
            nameof(Setting.CommitDiffWrapLines),
            false,
            GetJsonTypeInfo<bool>());

        public static readonly SettingDefinition<bool> CommitDiffIgnoreWhitespace = new(
            nameof(Setting.CommitDiffIgnoreWhitespace),
            false,
            GetJsonTypeInfo<bool>());

        public static readonly SettingDefinition<bool> CommitGraphRefsPanelOpen = new(
            nameof(Setting.CommitGraphRefsPanelOpen),
            true,
            GetJsonTypeInfo<bool>());

        public static readonly SettingDefinition<RemotePrimaryAction> RemotePrimaryAction = new(
            nameof(Setting.RemotePrimaryAction),
            Settings.RemotePrimaryAction.Fetch,
            GetJsonTypeInfo<RemotePrimaryAction>());

        private static readonly IReadOnlyDictionary<Setting, ISettingDefinition> Definitions =
            new Dictionary<Setting, ISettingDefinition>
            {
                [Setting.CurrentGitRepositoryId] = CurrentGitRepositoryId,
                [Setting.Theme] = Theme,
                [Setting.LightTheme] = LightTheme,
                [Setting.DarkTheme] = DarkTheme,
                [Setting.Font] = Font,
                [Setting.UiFont] = UiFont,
                [Setting.CodeFont] = CodeFont,
                [Setting.LightUiFont] = LightUiFont,
                [Setting.LightCodeFont] = LightCodeFont,
                [Setting.DarkUiFont] = DarkUiFont,
                [Setting.DarkCodeFont] = DarkCodeFont,
                [Setting.LightAccent] = LightAccent,
                [Setting.LightBackground] = LightBackground,
                [Setting.LightForeground] = LightForeground,
                [Setting.DarkAccent] = DarkAccent,
                [Setting.DarkBackground] = DarkBackground,
                [Setting.DarkForeground] = DarkForeground,
                [Setting.CommitDiffViewMode] = CommitDiffViewMode,
                [Setting.CommitDiffLineDisplayMode] = CommitDiffLineDisplayMode,
                [Setting.CommitDiffContextLines] = CommitDiffContextLines,
                [Setting.CommitDiffWrapLines] = CommitDiffWrapLines,
                [Setting.CommitDiffIgnoreWhitespace] = CommitDiffIgnoreWhitespace,
                [Setting.CommitGraphRefsPanelOpen] = CommitGraphRefsPanelOpen,
                [Setting.RemotePrimaryAction] = RemotePrimaryAction,
            };

        public static Dictionary<Setting, ISettingDefinition> GetAllDefinitions()
        {
            return new Dictionary<Setting, ISettingDefinition>(Definitions);
        }

        public static bool TryGetDefinition(Setting setting, out ISettingDefinition definition)
        {
            return Definitions.TryGetValue(setting, out definition!);
        }

        private static JsonTypeInfo<T> GetJsonTypeInfo<T>()
        {
            var typeInfo = SettingsJsonSerializerContext.Default.GetTypeInfo(typeof(T));
            if (typeInfo is JsonTypeInfo<T> typedTypeInfo)
            {
                return typedTypeInfo;
            }

            throw new InvalidOperationException($"No JSON type info registered for setting value type {typeof(T)}.");
        }
    }
}
