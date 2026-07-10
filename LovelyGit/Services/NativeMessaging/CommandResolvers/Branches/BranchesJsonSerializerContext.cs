using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RenameBranchCommandArguments))]
[JsonSerializable(typeof(DeleteBranchCommandArguments))]
[JsonSerializable(typeof(CheckoutRemoteBranchCommandArguments))]
[JsonSerializable(typeof(DeleteRemoteBranchCommandArguments))]
[JsonSerializable(typeof(PushBranchCommandArguments))]
[JsonSerializable(typeof(ManageBranchUpstreamCommandArguments))]
[JsonSerializable(typeof(CreateBranchFromTagCommandArguments))]
internal partial class BranchesJsonSerializerContext : JsonSerializerContext;
