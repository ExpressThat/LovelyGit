using ExpressThat.LazyGit;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(CommsHubCommand<JsonElement>))]
    [JsonSerializable(typeof(EmptyCommandArguments))]
    [JsonSerializable(typeof(CommitGraphCommandArguments))]
    [JsonSerializable(typeof(GetSettingsCommandArguments))]
    [JsonSerializable(typeof(SetSettingsCommandArguments))]
    [JsonSerializable(typeof(CommandResponse<List<KnownGitRepository>>))]
    [JsonSerializable(typeof(CommandResponse<CommitGraphResponse>))]
    [JsonSerializable(typeof(CommandResponse<JsonElement>))]
    [JsonSerializable(typeof(CommandResponse<Dictionary<Setting, JsonElement>>))]
    public partial class CommandReponseJsonSerializerContext : JsonSerializerContext
    {
    }
}
