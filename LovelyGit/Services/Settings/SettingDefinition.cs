using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings;
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

        public static readonly SettingDefinition<bool> AiFeaturesEnabled = new(
            nameof(Setting.AiFeaturesEnabled),
            false,
            GetJsonTypeInfo<bool>());

        public static readonly SettingDefinition<AiComputeDevice> AiComputeDevice = new(
            nameof(Setting.AiComputeDevice),
            AiHardwareDefaults.HasGpu() ? Settings.AiComputeDevice.Gpu : Settings.AiComputeDevice.Cpu,
            GetJsonTypeInfo<AiComputeDevice>());

        public static readonly SettingDefinition<AiModel> AiModel = new(
            nameof(Setting.AiModel),
            Settings.AiModel.Llama32_3B,
            GetJsonTypeInfo<AiModel>());

        public static readonly SettingDefinition<int> AiContextSize = new(
            nameof(Setting.AiContextSize),
            8192,
            GetJsonTypeInfo<int>());

        public static readonly SettingDefinition<int> AiLlamaRawDiffContextPercent = new(
            nameof(Setting.AiLlamaRawDiffContextPercent),
            50,
            GetJsonTypeInfo<int>());

        public static readonly SettingDefinition<int> AiGemmaRawDiffContextPercent = new(
            nameof(Setting.AiGemmaRawDiffContextPercent),
            30,
            GetJsonTypeInfo<int>());

        public static readonly SettingDefinition<int> AiSummaryContextPercent = new(
            nameof(Setting.AiSummaryContextPercent),
            20,
            GetJsonTypeInfo<int>());

        private static readonly IReadOnlyDictionary<Setting, ISettingDefinition> Definitions =
            new Dictionary<Setting, ISettingDefinition>
            {
                [Setting.CurrentGitRepositoryId] = CurrentGitRepositoryId,
                [Setting.Theme] = Theme,
                [Setting.CommitDiffViewMode] = CommitDiffViewMode,
                [Setting.CommitDiffLineDisplayMode] = CommitDiffLineDisplayMode,
                [Setting.CommitDiffContextLines] = CommitDiffContextLines,
                [Setting.CommitDiffWrapLines] = CommitDiffWrapLines,
                [Setting.AiFeaturesEnabled] = AiFeaturesEnabled,
                [Setting.AiComputeDevice] = AiComputeDevice,
                [Setting.AiModel] = AiModel,
                [Setting.AiContextSize] = AiContextSize,
                [Setting.AiLlamaRawDiffContextPercent] = AiLlamaRawDiffContextPercent,
                [Setting.AiGemmaRawDiffContextPercent] = AiGemmaRawDiffContextPercent,
                [Setting.AiSummaryContextPercent] = AiSummaryContextPercent,
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

    internal static class AiHardwareDefaults
    {
        public static bool HasGpu()
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                return Environment.UserInteractive;
            }

            if (!OperatingSystem.IsLinux())
            {
                return false;
            }

            return HasLinuxGpuDevice("/dev/dri") || Directory.Exists("/proc/driver/nvidia/gpus");
        }

        private static bool HasLinuxGpuDevice(string path)
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            try
            {
                return Directory.EnumerateFileSystemEntries(path, "renderD*").Any()
                    || Directory.EnumerateFileSystemEntries(path, "card*").Any();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
