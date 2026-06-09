using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.Hubs.Commands;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.WorkingTree;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WorkingTreeChangeGroup))]
[JsonSerializable(typeof(WorkingTreeChangedFile))]
[JsonSerializable(typeof(WorkingTreeChangesResponse))]
[JsonSerializable(typeof(WorkingTreeChangedNotification))]
[JsonSerializable(typeof(CommitGraphChangedNotification))]
[JsonSerializable(typeof(GetWorkingTreeChangesCommandArguments))]
[JsonSerializable(typeof(UpdateWorkingTreeIndexCommandArguments))]
[JsonSerializable(typeof(StageWorkingTreeLineCommandArguments))]
[JsonSerializable(typeof(CommitStagedChangesCommandArguments))]
[JsonSerializable(typeof(GetWorkingTreeFileDiffArguments))]
[JsonSerializable(typeof(CommitDiffViewMode))]
[JsonSerializable(typeof(CommitFileDiffSyntaxSpan))]
[JsonSerializable(typeof(CommitFileDiffChangeSpan))]
[JsonSerializable(typeof(CommitFileDiffLine))]
[JsonSerializable(typeof(CommitFileDiffResponse))]
[JsonSerializable(typeof(CommandResponse<WorkingTreeChangesResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitFileDiffResponse>))]
internal partial class WorkingTreeJsonSerializerContext : JsonSerializerContext
{
}
