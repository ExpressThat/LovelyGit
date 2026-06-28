using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CommitStats))]
[JsonSerializable(typeof(CommitInfo))]
[JsonSerializable(typeof(CommitRefInfo))]
[JsonSerializable(typeof(CommitRefKind))]
[JsonSerializable(typeof(CommitChangedFile))]
[JsonSerializable(typeof(CommitDetailsResponse))]
[JsonSerializable(typeof(CommitDiffViewMode))]
[JsonSerializable(typeof(CommitFileDiffSyntaxSpan))]
[JsonSerializable(typeof(CommitFileDiffChangeSpan))]
[JsonSerializable(typeof(CommitFileDiffLine))]
[JsonSerializable(typeof(CommitFileDiffResponse))]
[JsonSerializable(typeof(CommitPatchResponse))]
[JsonSerializable(typeof(CommitLaneEdge))]
[JsonSerializable(typeof(CommitGraphRow))]
[JsonSerializable(typeof(CommitGraphResponse))]
[JsonSerializable(typeof(CommitGraphCommandArguments))]
[JsonSerializable(typeof(CancelCommitDiffPreparationCommandArguments))]
[JsonSerializable(typeof(GetCommitDetailsCommandArguments))]
[JsonSerializable(typeof(GetCommitFileDiffCommandArguments))]
[JsonSerializable(typeof(GetCommitPatchCommandArguments))]
[JsonSerializable(typeof(CommandResponse<CommitGraphResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitDetailsResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitFileDiffResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitPatchResponse>))]
internal partial class CommitGraphJsonSerializerContext : JsonSerializerContext
{
}
