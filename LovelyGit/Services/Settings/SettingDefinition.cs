using ExpressThat.LazyGit;
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

        private static readonly IReadOnlyDictionary<Setting, ISettingDefinition> Definitions =
            new Dictionary<Setting, ISettingDefinition>
            {
                [Setting.CurrentGitRepositoryId] = CurrentGitRepositoryId,
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
            var typeInfo = AppJsonSerializerContext.Default.GetTypeInfo(typeof(T));
            if (typeInfo is JsonTypeInfo<T> typedTypeInfo)
            {
                return typedTypeInfo;
            }

            throw new InvalidOperationException($"No JSON type info registered for setting value type {typeof(T)}.");
        }
    }
}
