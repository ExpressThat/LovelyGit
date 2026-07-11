using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreateTagAtCommitCommandArguments))]
[JsonSerializable(typeof(DeleteTagCommandArguments))]
[JsonSerializable(typeof(PushTagCommandArguments))]
[JsonSerializable(typeof(DeleteRemoteTagCommandArguments))]
internal partial class TagsJsonSerializerContext : JsonSerializerContext;
