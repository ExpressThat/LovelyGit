using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ResetCurrentBranchToCommitCommandArguments))]
internal partial class ResetJsonSerializerContext : JsonSerializerContext;
