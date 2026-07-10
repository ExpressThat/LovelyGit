using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WorkingTreeChangeGroup))]
[JsonSerializable(typeof(WorkingTreeChangedFile))]
[JsonSerializable(typeof(WorkingTreeChangeSummaryResponse))]
[JsonSerializable(typeof(WorkingTreeChangesResponse))]
[JsonSerializable(typeof(WorkingTreeChangedNotification))]
[JsonSerializable(typeof(CommitGraphChangedNotification))]
[JsonSerializable(typeof(GetWorkingTreeChangesCommandArguments))]
[JsonSerializable(typeof(RevealWorkingTreeFileCommandArguments))]
[JsonSerializable(typeof(UpdateWorkingTreeIndexCommandArguments))]
[JsonSerializable(typeof(DiscardWorkingTreeChangesCommandArguments))]
[JsonSerializable(typeof(StageWorkingTreeLineCommandArguments))]
[JsonSerializable(typeof(GetHeadCommitMessageCommandArguments))]
[JsonSerializable(typeof(CommitStagedChangesCommandArguments))]
[JsonSerializable(typeof(GitRemoteCommandArguments))]
[JsonSerializable(typeof(CheckoutBranchCommandArguments))]
[JsonSerializable(typeof(CreateBranchCommandArguments))]
[JsonSerializable(typeof(StashCommandArguments))]
[JsonSerializable(typeof(StashAction))]
[JsonSerializable(typeof(GitPullMode))]
[JsonSerializable(typeof(GetWorkingTreeFileDiffArguments))]
[JsonSerializable(typeof(CommitDiffViewMode))]
[JsonSerializable(typeof(CommitFileDiffSyntaxSpan))]
[JsonSerializable(typeof(CommitFileDiffChangeSpan))]
[JsonSerializable(typeof(CommitFileDiffLine))]
[JsonSerializable(typeof(CommitFileDiffResponse))]
[JsonSerializable(typeof(CommandResponse<WorkingTreeChangesResponse>))]
[JsonSerializable(typeof(CommandResponse<WorkingTreeChangeSummaryResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitFileDiffResponse>))]
[JsonSerializable(typeof(CommandResponse<HeadCommitMessageResponse>))]
internal partial class WorkingTreeJsonSerializerContext : JsonSerializerContext
{
}
