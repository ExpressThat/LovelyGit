using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Git.Patches;
using ExpressThat.LovelyGit.Services.Git.Submodules;
using ExpressThat.LovelyGit.Services.Git.RemoteSync;

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
[JsonSerializable(typeof(IgnoreWorkingTreePathCommandArguments))]
[JsonSerializable(typeof(GitIgnoreTarget))]
[JsonSerializable(typeof(GitIgnoreResult))]
[JsonSerializable(typeof(UpdateWorkingTreeIndexCommandArguments))]
[JsonSerializable(typeof(DiscardWorkingTreeChangesCommandArguments))]
[JsonSerializable(typeof(StageWorkingTreeLineCommandArguments))]
[JsonSerializable(typeof(StageWorkingTreeHunkCommandArguments))]
[JsonSerializable(typeof(GetHeadCommitMessageCommandArguments))]
[JsonSerializable(typeof(UndoLastCommitCommandArguments))]
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
[JsonSerializable(typeof(GetRemoteSyncStatusCommandArguments))]
[JsonSerializable(typeof(RemoteSyncStatusResponse))]
[JsonSerializable(typeof(GitPushMode))]
[JsonSerializable(typeof(GetRemotesCommandArguments))]
[JsonSerializable(typeof(ManageRemoteCommandArguments))]
[JsonSerializable(typeof(RemoteMutationAction))]
[JsonSerializable(typeof(GitRemote))]
[JsonSerializable(typeof(List<GitRemote>))]
[JsonSerializable(typeof(CheckoutBranchCommandArguments))]
[JsonSerializable(typeof(CheckoutCommitCommandArguments))]
[JsonSerializable(typeof(CreateBranchCommandArguments))]
[JsonSerializable(typeof(StashCommandArguments))]
[JsonSerializable(typeof(StashAction))]
[JsonSerializable(typeof(GitPullMode))]
[JsonSerializable(typeof(GetWorkingTreeFileDiffArguments))]
[JsonSerializable(typeof(GetConflictResolutionCommandArguments))]
[JsonSerializable(typeof(ResolveConflictCommandArguments))]
[JsonSerializable(typeof(OpenConflictInMergeToolCommandArguments))]
[JsonSerializable(typeof(ConflictResolutionSource))]
[JsonSerializable(typeof(ConflictSourceMetadata))]
[JsonSerializable(typeof(ConflictHunk))]
[JsonSerializable(typeof(ConflictFileVersion))]
[JsonSerializable(typeof(ConflictResolutionResponse))]
[JsonSerializable(typeof(CommitDiffViewMode))]
[JsonSerializable(typeof(CommitFileDiffSyntaxSpan))]
[JsonSerializable(typeof(CommitFileDiffChangeSpan))]
[JsonSerializable(typeof(CommitFileDiffLine))]
[JsonSerializable(typeof(CommitFileDiffResponse))]
[JsonSerializable(typeof(CommandResponse<WorkingTreeChangesResponse>))]
[JsonSerializable(typeof(CommandResponse<WorkingTreeChangeSummaryResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitFileDiffResponse>))]
[JsonSerializable(typeof(CommandResponse<ConflictResolutionResponse>))]
[JsonSerializable(typeof(CommandResponse<GitIgnoreResult>))]
[JsonSerializable(typeof(CommandResponse<EmptyCommandArguments>))]
[JsonSerializable(typeof(CommandResponse<HeadCommitMessageResponse>))]
[JsonSerializable(typeof(CommandResponse<List<GitRemote>>))]
[JsonSerializable(typeof(CommandResponse<RemoteSyncStatusResponse>))]
[JsonSerializable(typeof(CommandResponse<WorktreeDestinationResponse>))]
[JsonSerializable(typeof(CommandResponse<KnownGitRepository>))]
[JsonSerializable(typeof(CommandResponse<PatchPreviewResponse>))]
[JsonSerializable(typeof(CommandResponse<List<GitSubmodule>>))]
internal partial class WorkingTreeJsonSerializerContext : JsonSerializerContext
{
}
