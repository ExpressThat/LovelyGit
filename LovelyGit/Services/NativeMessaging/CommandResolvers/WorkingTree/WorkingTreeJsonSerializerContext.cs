using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Git.Patches;
using ExpressThat.LovelyGit.Services.Git.Submodules;

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
[JsonSerializable(typeof(ApplyPatchCommandArguments))]
[JsonSerializable(typeof(PatchFilePreview))]
[JsonSerializable(typeof(PatchPreviewResponse))]
[JsonSerializable(typeof(ManageSubmoduleCommandArguments))]
[JsonSerializable(typeof(SubmoduleAction))]
[JsonSerializable(typeof(SubmoduleState))]
[JsonSerializable(typeof(GitSubmodule))]
[JsonSerializable(typeof(List<GitSubmodule>))]
[JsonSerializable(typeof(CreateWorktreeCommandArguments))]
[JsonSerializable(typeof(ManageWorktreeCommandArguments))]
[JsonSerializable(typeof(WorktreeMutationAction))]
[JsonSerializable(typeof(WorktreeDestinationResponse))]
[JsonSerializable(typeof(GitRemoteCommandArguments))]
[JsonSerializable(typeof(GetRemotesCommandArguments))]
[JsonSerializable(typeof(ManageRemoteCommandArguments))]
[JsonSerializable(typeof(RemoteMutationAction))]
[JsonSerializable(typeof(GitRemote))]
[JsonSerializable(typeof(List<GitRemote>))]
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
[JsonSerializable(typeof(CommandResponse<List<GitRemote>>))]
[JsonSerializable(typeof(CommandResponse<WorktreeDestinationResponse>))]
[JsonSerializable(typeof(CommandResponse<KnownGitRepository>))]
[JsonSerializable(typeof(CommandResponse<PatchPreviewResponse>))]
[JsonSerializable(typeof(CommandResponse<List<GitSubmodule>>))]
internal partial class WorkingTreeJsonSerializerContext : JsonSerializerContext
{
}
