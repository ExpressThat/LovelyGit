using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NativeCommand<JsonElement>))]
[JsonSerializable(typeof(EmptyCommandArguments))]
[JsonSerializable(typeof(CommandResponseBase))]
[JsonSerializable(typeof(CommandResponse<JsonElement>))]
internal partial class CommandJsonSerializerContext : JsonSerializerContext
{
}
