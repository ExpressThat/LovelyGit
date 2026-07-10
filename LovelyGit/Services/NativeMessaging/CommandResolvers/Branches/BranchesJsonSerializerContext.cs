using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RenameBranchCommandArguments))]
[JsonSerializable(typeof(DeleteBranchCommandArguments))]
[JsonSerializable(typeof(PushBranchCommandArguments))]
[JsonSerializable(typeof(CreateBranchFromTagCommandArguments))]
internal partial class BranchesJsonSerializerContext : JsonSerializerContext;
