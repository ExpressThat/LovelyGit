using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreateBranchFromCommitCommandArguments))]
[JsonSerializable(typeof(DeleteBranchCommandArguments))]
[JsonSerializable(typeof(PushBranchCommandArguments))]
[JsonSerializable(typeof(RenameBranchCommandArguments))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class BranchesJsonSerializerContext : JsonSerializerContext
{
}
