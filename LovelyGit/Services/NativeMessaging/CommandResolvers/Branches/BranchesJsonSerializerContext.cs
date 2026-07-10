using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RenameBranchCommandArguments))]
[JsonSerializable(typeof(DeleteBranchCommandArguments))]
[JsonSerializable(typeof(PushBranchCommandArguments))]
internal partial class BranchesJsonSerializerContext : JsonSerializerContext;
