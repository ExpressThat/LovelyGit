using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpressThat.LazyGit;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CommitStats))]
[JsonSerializable(typeof(CommitInfo))]
[JsonSerializable(typeof(CommitLaneEdge))]
[JsonSerializable(typeof(CommitGraphRow))]
[JsonSerializable(typeof(CommitGraphResponse))]
[JsonSerializable(typeof(CommsHubCommand<JsonElement>))]
[JsonSerializable(typeof(EmptyCommandArguments))]
[JsonSerializable(typeof(CommitGraphCommandArguments))]
[JsonSerializable(typeof(GetSettingsCommandArguments))]
[JsonSerializable(typeof(SetSettingsCommandArguments))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(CommandResponseBase))]
[JsonSerializable(typeof(CommandResponse<CommitGraphResponse>))]
[JsonSerializable(typeof(CommandResponse<JsonElement>))]
[JsonSerializable(typeof(CommandResponse<Dictionary<Setting, JsonElement>>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
