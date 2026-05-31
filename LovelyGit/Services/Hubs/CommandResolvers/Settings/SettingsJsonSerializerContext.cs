using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GetSettingsCommandArguments))]
[JsonSerializable(typeof(SetSettingsCommandArguments))]
[JsonSerializable(typeof(SetMultipleSettingsCommandArguments))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(AppTheme))]
[JsonSerializable(typeof(CommandResponse<Dictionary<Setting, JsonElement>>))]
internal partial class SettingsJsonSerializerContext : JsonSerializerContext
{
}
