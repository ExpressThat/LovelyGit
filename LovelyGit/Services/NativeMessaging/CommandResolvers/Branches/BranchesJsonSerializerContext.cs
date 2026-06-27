using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreateBranchFromCommitCommandArguments))]
internal partial class BranchesJsonSerializerContext : JsonSerializerContext
{
}
