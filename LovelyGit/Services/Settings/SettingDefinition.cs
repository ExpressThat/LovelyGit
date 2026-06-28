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

        public static readonly SettingDefinition<AppTheme> Theme = new(
            nameof(Setting.Theme),
            AppTheme.System,
            GetJsonTypeInfo<AppTheme>());

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

        public static readonly SettingDefinition<ConflictFileViewMode> ConflictFileViewMode = new(
            nameof(Setting.ConflictFileViewMode),
            Settings.ConflictFileViewMode.Path,
            GetJsonTypeInfo<ConflictFileViewMode>());

        private static readonly IReadOnlyDictionary<Setting, ISettingDefinition> Definitions =
            new Dictionary<Setting, ISettingDefinition>
            {
                [Setting.CurrentGitRepositoryId] = CurrentGitRepositoryId,
                [Setting.Theme] = Theme,
                [Setting.CommitDiffViewMode] = CommitDiffViewMode,
                [Setting.CommitDiffLineDisplayMode] = CommitDiffLineDisplayMode,
                [Setting.CommitDiffContextLines] = CommitDiffContextLines,
                [Setting.CommitDiffWrapLines] = CommitDiffWrapLines,
                [Setting.CommitDiffIgnoreWhitespace] = CommitDiffIgnoreWhitespace,
                [Setting.CommitGraphRefsPanelOpen] = CommitGraphRefsPanelOpen,
                [Setting.RemotePrimaryAction] = RemotePrimaryAction,
                [Setting.ConflictFileViewMode] = ConflictFileViewMode,
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
