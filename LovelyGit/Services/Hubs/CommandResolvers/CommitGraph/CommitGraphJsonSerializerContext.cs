using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CommitStats))]
[JsonSerializable(typeof(CommitInfo))]
[JsonSerializable(typeof(CommitChangedFile))]
[JsonSerializable(typeof(CommitDetailsResponse))]
[JsonSerializable(typeof(CommitLaneEdge))]
[JsonSerializable(typeof(CommitGraphRow))]
[JsonSerializable(typeof(CommitGraphResponse))]
[JsonSerializable(typeof(CommitGraphCommandArguments))]
[JsonSerializable(typeof(GetCommitDetailsCommandArguments))]
[JsonSerializable(typeof(CommandResponse<CommitGraphResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitDetailsResponse>))]
internal partial class CommitGraphJsonSerializerContext : JsonSerializerContext
{
}
