using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CheckoutCommitDetachedCommandArguments))]
[JsonSerializable(typeof(CheckoutBranchCommandArguments))]
[JsonSerializable(typeof(CheckoutRemoteBranchCommandArguments))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class CheckoutJsonSerializerContext : JsonSerializerContext
{
}
