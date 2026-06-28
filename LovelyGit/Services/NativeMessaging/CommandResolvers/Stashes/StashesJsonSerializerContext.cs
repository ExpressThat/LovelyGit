using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Stashes;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StashChangesCommandArguments))]
[JsonSerializable(typeof(StashReferenceCommandArguments))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class StashesJsonSerializerContext : JsonSerializerContext
{
}
