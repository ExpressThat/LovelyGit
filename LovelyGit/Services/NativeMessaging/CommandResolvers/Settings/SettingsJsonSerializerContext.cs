using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Settings;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GetSettingsCommandArguments))]
[JsonSerializable(typeof(SetSettingsCommandArguments))]
[JsonSerializable(typeof(SetMultipleSettingsCommandArguments))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(CommitDiffViewMode))]
[JsonSerializable(typeof(CommitDiffLineDisplayMode))]
[JsonSerializable(typeof(RemotePrimaryAction))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(Dictionary<Setting, JsonElement>))]
[JsonSerializable(typeof(CommandResponse<Dictionary<Setting, JsonElement>>))]
internal partial class SettingsJsonSerializerContext : JsonSerializerContext
{
}
