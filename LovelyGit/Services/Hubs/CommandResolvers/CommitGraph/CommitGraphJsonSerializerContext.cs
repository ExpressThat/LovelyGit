using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CommitStats))]
[JsonSerializable(typeof(CommitInfo))]
[JsonSerializable(typeof(CommitChangedFile))]
[JsonSerializable(typeof(CommitDetailsResponse))]
[JsonSerializable(typeof(CommitDiffViewMode))]
[JsonSerializable(typeof(CommitFileDiffSyntaxSpan))]
[JsonSerializable(typeof(CommitFileDiffChangeSpan))]
[JsonSerializable(typeof(CommitFileDiffLine))]
[JsonSerializable(typeof(CommitFileDiffResponse))]
[JsonSerializable(typeof(CommitLaneEdge))]
[JsonSerializable(typeof(CommitGraphRow))]
[JsonSerializable(typeof(CommitGraphResponse))]
[JsonSerializable(typeof(CommitGraphCommandArguments))]
[JsonSerializable(typeof(CancelCommitDiffPreparationCommandArguments))]
[JsonSerializable(typeof(GetCommitDetailsCommandArguments))]
[JsonSerializable(typeof(GetCommitFileDiffCommandArguments))]
[JsonSerializable(typeof(CommandResponse<CommitGraphResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitDetailsResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitFileDiffResponse>))]
internal partial class CommitGraphJsonSerializerContext : JsonSerializerContext
{
}
