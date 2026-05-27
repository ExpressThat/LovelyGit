using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json.Serialization;

namespace ExpressThat.LazyGit;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(ApiErrorResponse))]
[JsonSerializable(typeof(CommitGraphPageRequest))]
[JsonSerializable(typeof(CommitStats))]
[JsonSerializable(typeof(CommitInfo))]
[JsonSerializable(typeof(CommitLaneEdge))]
[JsonSerializable(typeof(CommitGraphRow))]
[JsonSerializable(typeof(CommitGraphResponse))]
[JsonSerializable(typeof(CommsHubCommand))]
[JsonSerializable(typeof(CommandResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
