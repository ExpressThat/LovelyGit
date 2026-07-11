using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.Lfs;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Lfs;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GetGitLfsStateCommandArguments))]
[JsonSerializable(typeof(ManageGitLfsCommandArguments))]
[JsonSerializable(typeof(GitLfsAction))]
[JsonSerializable(typeof(LfsRepositoryState))]
[JsonSerializable(typeof(CommandResponse<LfsRepositoryState>))]
internal partial class LfsJsonSerializerContext : JsonSerializerContext
{
}
