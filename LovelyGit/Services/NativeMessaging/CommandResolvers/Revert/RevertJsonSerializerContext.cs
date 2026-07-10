using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Revert;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RevertCommitCommandArguments))]
[JsonSerializable(typeof(CommandResponse<RepositoryOperationCommandResponse>))]
internal partial class RevertJsonSerializerContext : JsonSerializerContext;
