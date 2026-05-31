using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CommsHubCommand<JsonElement>))]
[JsonSerializable(typeof(EmptyCommandArguments))]
[JsonSerializable(typeof(CommandResponseBase))]
[JsonSerializable(typeof(CommandResponse<JsonElement>))]
internal partial class CommandJsonSerializerContext : JsonSerializerContext
{
}
