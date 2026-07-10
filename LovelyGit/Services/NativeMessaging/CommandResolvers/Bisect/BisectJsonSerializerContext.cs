using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.Bisect;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Bisect;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GetBisectStateCommandArguments))]
[JsonSerializable(typeof(ManageBisectCommandArguments))]
[JsonSerializable(typeof(GitBisectAction))]
[JsonSerializable(typeof(GitBisectState))]
[JsonSerializable(typeof(CommandResponse<GitBisectState>))]
internal partial class BisectJsonSerializerContext : JsonSerializerContext
{
}
