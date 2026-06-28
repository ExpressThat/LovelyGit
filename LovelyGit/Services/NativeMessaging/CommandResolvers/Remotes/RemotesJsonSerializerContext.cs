using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Remotes;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitRemote))]
[JsonSerializable(typeof(GetRepositoryRemotesCommandArguments))]
[JsonSerializable(typeof(CommandResponse<List<GitRemote>>))]
internal partial class RemotesJsonSerializerContext : JsonSerializerContext
{
}
