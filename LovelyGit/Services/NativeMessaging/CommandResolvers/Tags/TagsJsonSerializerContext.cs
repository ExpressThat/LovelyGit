using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreateTagAtCommitCommandArguments))]
[JsonSerializable(typeof(DeleteTagCommandArguments))]
[JsonSerializable(typeof(PushTagCommandArguments))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class TagsJsonSerializerContext : JsonSerializerContext
{
}
