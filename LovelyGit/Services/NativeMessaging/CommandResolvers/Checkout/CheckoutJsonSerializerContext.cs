using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CheckoutBranchCommandArguments))]
[JsonSerializable(typeof(CheckoutTagCommandArguments))]
internal partial class CheckoutJsonSerializerContext : JsonSerializerContext;
