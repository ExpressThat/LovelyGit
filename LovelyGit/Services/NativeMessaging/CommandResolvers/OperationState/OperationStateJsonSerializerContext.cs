using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.OperationState;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.OperationState;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GetGitOperationStateCommandArguments))]
[JsonSerializable(typeof(GitOperationKind))]
[JsonSerializable(typeof(GitOperationState))]
[JsonSerializable(typeof(CommandResponse<GitOperationState>))]
internal partial class OperationStateJsonSerializerContext : JsonSerializerContext
{
}
