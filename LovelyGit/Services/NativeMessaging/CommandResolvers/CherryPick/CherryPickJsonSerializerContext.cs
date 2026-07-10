using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CherryPickCommitCommandArguments))]
[JsonSerializable(typeof(CommandResponse<RepositoryOperationCommandResponse>))]
internal partial class CherryPickJsonSerializerContext : JsonSerializerContext;
