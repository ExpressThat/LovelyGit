using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.SparseCheckout;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.SparseCheckout;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GetSparseCheckoutStateCommandArguments))]
[JsonSerializable(typeof(ManageSparseCheckoutCommandArguments))]
[JsonSerializable(typeof(SparseCheckoutState))]
[JsonSerializable(typeof(SparseCheckoutAction))]
[JsonSerializable(typeof(CommandResponse<SparseCheckoutState>))]
internal partial class SparseCheckoutJsonSerializerContext : JsonSerializerContext;
