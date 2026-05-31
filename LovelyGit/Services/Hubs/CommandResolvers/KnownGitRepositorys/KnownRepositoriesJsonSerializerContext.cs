using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.KnownRepository;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CommandResponse<List<KnownGitRepository>>))]
[JsonSerializable(typeof(CommandResponse<KnownGitRepository?>))]
internal partial class KnownRepositoriesJsonSerializerContext : JsonSerializerContext
{
}
