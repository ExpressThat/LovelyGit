using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RemoveKnownGitRepositorysCommandArguments))]
[JsonSerializable(typeof(List<KnownGitRepository>))]
[JsonSerializable(typeof(KnownGitRepository))]
[JsonSerializable(typeof(CommandResponse<List<KnownGitRepository>>))]
[JsonSerializable(typeof(CommandResponse<KnownGitRepository?>))]
internal partial class KnownRepositoriesJsonSerializerContext : JsonSerializerContext
{
}
