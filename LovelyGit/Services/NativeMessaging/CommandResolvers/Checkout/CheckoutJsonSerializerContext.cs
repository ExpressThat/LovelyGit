using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;

[JsonSerializable(typeof(CheckoutCommitDetachedCommandArguments))]
[JsonSerializable(typeof(CheckoutBranchCommandArguments))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
internal partial class CheckoutJsonSerializerContext : JsonSerializerContext
{
}
