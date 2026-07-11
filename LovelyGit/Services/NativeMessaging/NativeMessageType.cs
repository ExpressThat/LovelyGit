using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Bisect;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Configuration;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Revert;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Settings;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using ExpressThat.LovelyGit.Services.Git.Reflog;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using ExpressThat.LovelyGit.Services.Git.FileHistory;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using ExpressThat.LovelyGit.Services.Settings;
using ExpressThat.LovelyGit.Services.Git.BranchComparison;
using ExpressThat.LovelyGit.Services.Git.Patches;
using ExpressThat.LovelyGit.Services.Git.Submodules;
using ExpressThat.LovelyGit.Services.Git.Bisect;
using ExpressThat.LovelyGit.Services.Git.Configuration;
using ExpressThat.LovelyGit.Services.Git.RemoteSync;
using System.Text.Json;

namespace ExpressThat.LovelyGit.Services.NativeMessaging
{
    [JsonConverter(typeof(JsonStringEnumConverter<NativeMessageType>))]
    public enum NativeMessageType
    {
        // Repository discovery, tabs, local launching, and cloning.
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(List<KnownGitRepository>))]
        KnownGitRepositorys,
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(KnownGitRepository))]
        AddKnownGitRepositorys,
        [NativeMessageContract(typeof(RemoveKnownGitRepositorysCommandArguments))]
        RemoveKnownGitRepositorys,
        [NativeMessageContract(typeof(RevealKnownGitRepositoryCommandArguments), typeof(EmptyCommandArguments))]
        RevealKnownGitRepository,
        [NativeMessageContract(typeof(OpenRepositoryTerminalCommandArguments), typeof(EmptyCommandArguments))]
        OpenRepositoryTerminal,
        [NativeMessageContract(typeof(OpenRemoteWebResourceCommandArguments), typeof(EmptyCommandArguments))]
        OpenRemoteWebResource,
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(CloneDestinationResponse))]
        ChooseCloneDestination,
        [NativeMessageContract(typeof(CloneRepositoryCommandArguments), typeof(KnownGitRepository))]
        CloneRepository,
        [NativeMessageContract(typeof(CancelCloneRepositoryCommandArguments))]
        CancelCloneRepository,
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(CloneDestinationResponse))]
        ChooseRepositoryDestination,
        [NativeMessageContract(typeof(InitializeRepositoryCommandArguments), typeof(KnownGitRepository))]
        InitializeRepository,

        // Fast native commit graph, commit metadata, patch, and diff reads.
        [NativeMessageContract(typeof(CommitGraphCommandArguments), typeof(CommitGraphResponse))]
        CommitGraph,
        [NativeMessageContract(typeof(GetRepositoryRefsCommandArguments), typeof(RepositoryRefsResponse))]
        GetRepositoryRefs,
        [NativeMessageContract(typeof(GetBranchComparisonCommandArguments), typeof(BranchComparisonResponse))]
        GetBranchComparison,
        [NativeMessageContract(typeof(GetReflogCommandArguments), typeof(GitReflogResponse))]
        GetReflog,
        [NativeMessageContract(typeof(SearchCommitsCommandArguments), typeof(CommitSearchResponse))]
        SearchCommits,
        [NativeMessageContract(typeof(GetFileHistoryCommandArguments), typeof(FileHistoryResponse))]
        GetFileHistory,
        [NativeMessageContract(typeof(GetFileBlameCommandArguments), typeof(FileBlameResponse))]
        GetFileBlame,
        [NativeMessageContract(typeof(GetCommitDetailsCommandArguments), typeof(CommitDetailsResponse))]
        GetCommitDetails,
        [NativeMessageContract(typeof(GetCommitFileDiffCommandArguments), typeof(CommitFileDiffResponse))]
        GetCommitFileDiff,
        [NativeMessageContract(typeof(GetCommitPatchCommandArguments), typeof(CommitPatchResponse))]
        GetCommitPatch,
        [NativeMessageContract(typeof(GetCommitPatchCommandArguments), typeof(CommitPatchExportResponse))]
        SaveCommitPatch,

        // Native bisect session state and explicit Git CLI progression.
        [NativeMessageContract(typeof(GetBisectStateCommandArguments), typeof(GitBisectState))]
        GetBisectState,
        [NativeMessageContract(typeof(ManageBisectCommandArguments), typeof(GitBisectState))]
        ManageBisect,

        // Native effective Git configuration reads and repository-local mutations.
        [NativeMessageContract(typeof(GetCommitIdentityCommandArguments), typeof(GitCommitIdentity))]
        GetCommitIdentity,
        [NativeMessageContract(typeof(ManageCommitIdentityCommandArguments), typeof(GitCommitIdentity))]
        ManageCommitIdentity,

        // Working-tree reads and file/index mutations.
        [NativeMessageContract(typeof(GetWorkingTreeChangesCommandArguments), typeof(WorkingTreeChangeSummaryResponse))]
        GetWorkingTreeChangeSummary,
        [NativeMessageContract(typeof(GetWorkingTreeChangesCommandArguments), typeof(WorkingTreeChangesResponse))]
        GetWorkingTreeChanges,
        [NativeMessageContract(typeof(GetWorkingTreeFileDiffArguments), typeof(CommitFileDiffResponse))]
        GetWorkingTreeFileDiff,
        [NativeMessageContract(typeof(GetRemoteSyncStatusCommandArguments), typeof(RemoteSyncStatusResponse))]
        GetRemoteSyncStatus,
        [NativeMessageContract(typeof(GetConflictResolutionCommandArguments), typeof(ConflictResolutionResponse))]
        GetConflictResolution,
        [NativeMessageContract(typeof(ResolveConflictCommandArguments))]
        ResolveConflict,
        [NativeMessageContract(typeof(OpenConflictInMergeToolCommandArguments))]
        OpenConflictInMergeTool,
        [NativeMessageContract(typeof(RevealWorkingTreeFileCommandArguments), typeof(EmptyCommandArguments))]
        RevealWorkingTreeFile,
        [NativeMessageContract(typeof(IgnoreWorkingTreePathCommandArguments), typeof(GitIgnoreResult))]
        IgnoreWorkingTreePath,
        [NativeMessageContract(typeof(UpdateWorkingTreeIndexCommandArguments))]
        StageWorkingTreeFiles,
        [NativeMessageContract(typeof(UpdateWorkingTreeIndexCommandArguments))]
        UnstageWorkingTreeFiles,
        [NativeMessageContract(typeof(DiscardWorkingTreeChangesCommandArguments))]
        DiscardWorkingTreeChanges,
        [NativeMessageContract(typeof(StageWorkingTreeLineCommandArguments))]
        StageWorkingTreeLine,
        [NativeMessageContract(typeof(StageWorkingTreeLineCommandArguments))]
        UnstageWorkingTreeLine,
        [NativeMessageContract(typeof(StageWorkingTreeHunkCommandArguments))]
        StageWorkingTreeHunk,
        [NativeMessageContract(typeof(StageWorkingTreeHunkCommandArguments))]
        UnstageWorkingTreeHunk,
        [NativeMessageContract(typeof(GetHeadCommitMessageCommandArguments), typeof(HeadCommitMessageResponse))]
        GetHeadCommitMessage,
        [NativeMessageContract(typeof(UndoLastCommitCommandArguments), typeof(HeadCommitMessageResponse))]
        UndoLastCommit,
        [NativeMessageContract(typeof(CommitStagedChangesCommandArguments), typeof(EmptyCommandArguments))]
        CommitStagedChanges,
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(PatchPreviewResponse))]
        ChoosePatchFile,
        [NativeMessageContract(typeof(ApplyPatchCommandArguments), typeof(EmptyCommandArguments))]
        ApplyPatch,

        // Native submodule discovery and explicit lifecycle mutations.
        [NativeMessageContract(typeof(GetRemotesCommandArguments), typeof(List<GitSubmodule>))]
        GetSubmodules,
        [NativeMessageContract(typeof(ManageSubmoduleCommandArguments), typeof(EmptyCommandArguments))]
        ManageSubmodule,

        // Linked worktree creation, navigation, locking, and removal.
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(WorktreeDestinationResponse))]
        ChooseWorktreeDestination,
        [NativeMessageContract(typeof(CreateWorktreeCommandArguments), typeof(EmptyCommandArguments))]
        CreateWorktree,
        [NativeMessageContract(typeof(ManageWorktreeCommandArguments), typeof(KnownGitRepository))]
        ManageWorktree,

        // Remote synchronization and branch/ref mutations.
        [NativeMessageContract(typeof(GetRemotesCommandArguments), typeof(List<GitRemote>))]
        GetRemotes,
        [NativeMessageContract(typeof(ManageRemoteCommandArguments), typeof(EmptyCommandArguments))]
        ManageRemote,
        [NativeMessageContract(typeof(GitRemoteCommandArguments))]
        FetchRepository,
        [NativeMessageContract(typeof(GitRemoteCommandArguments))]
        PullRepository,
        [NativeMessageContract(typeof(GitRemoteCommandArguments))]
        PushRepository,
        [NativeMessageContract(typeof(CheckoutBranchCommandArguments), typeof(EmptyCommandArguments))]
        CheckoutBranch,
        [NativeMessageContract(typeof(CheckoutRemoteBranchCommandArguments), typeof(EmptyCommandArguments))]
        CheckoutRemoteBranch,
        [NativeMessageContract(typeof(CreateBranchCommandArguments), typeof(EmptyCommandArguments))]
        CreateBranch,
        [NativeMessageContract(typeof(RenameBranchCommandArguments), typeof(EmptyCommandArguments))]
        RenameBranch,
        [NativeMessageContract(typeof(DeleteBranchCommandArguments), typeof(EmptyCommandArguments))]
        DeleteBranch,
        [NativeMessageContract(typeof(DeleteRemoteBranchCommandArguments), typeof(EmptyCommandArguments))]
        DeleteRemoteBranch,
        [NativeMessageContract(typeof(PushBranchCommandArguments), typeof(EmptyCommandArguments))]
        PushBranch,
        [NativeMessageContract(typeof(ManageBranchUpstreamCommandArguments), typeof(EmptyCommandArguments))]
        ManageBranchUpstream,
        [NativeMessageContract(typeof(StashCommandArguments))]
        ManageStash,
        [NativeMessageContract(typeof(CreateTagAtCommitCommandArguments), typeof(EmptyCommandArguments))]
        CreateTagAtCommit,
        [NativeMessageContract(typeof(DeleteTagCommandArguments), typeof(EmptyCommandArguments))]
        DeleteTag,
        [NativeMessageContract(typeof(PushTagCommandArguments), typeof(EmptyCommandArguments))]
        PushTag,

        // Multi-step repository operations and their conflict lifecycle.
        [NativeMessageContract(typeof(CherryPickCommitCommandArguments), typeof(RepositoryOperationCommandResponse))]
        CherryPickCommit,
        [NativeMessageContract(typeof(RevertCommitCommandArguments), typeof(RepositoryOperationCommandResponse))]
        RevertCommit,
        [NativeMessageContract(typeof(ResetCurrentBranchToCommitCommandArguments), typeof(EmptyCommandArguments))]
        ResetCurrentBranchToCommit,
        [NativeMessageContract(typeof(MergeBranchIntoCurrentCommandArguments), typeof(RepositoryOperationCommandResponse))]
        MergeBranchIntoCurrent,
        [NativeMessageContract(typeof(RebaseCurrentBranchOntoBranchCommandArguments), typeof(RepositoryOperationCommandResponse))]
        RebaseCurrentBranchOntoBranch,
        [NativeMessageContract(typeof(GetInteractiveRebasePlanCommandArguments), typeof(InteractiveRebasePlanResponse))]
        GetInteractiveRebasePlan,
        [NativeMessageContract(typeof(StartInteractiveRebaseCommandArguments), typeof(RepositoryOperationCommandResponse))]
        StartInteractiveRebase,
        [NativeMessageContract(typeof(GetRepositoryOperationStateCommandArguments), typeof(RepositoryOperationStateResponse))]
        GetRepositoryOperationState,
        [NativeMessageContract(typeof(RepositoryOperationCommandArguments), typeof(RepositoryOperationCommandResponse))]
        ContinueRepositoryOperation,
        [NativeMessageContract(typeof(RepositoryOperationCommandArguments))]
        AbortRepositoryOperation,

        // User preferences and bulk settings hydration.
        [NativeMessageContract(typeof(GetSettingsCommandArguments), typeof(JsonElement))]
        GetSetting,
        [NativeMessageContract(typeof(SetSettingsCommandArguments))]
        SetSetting,
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(Dictionary<Setting, JsonElement>))]
        GetAllSettings,
        [NativeMessageContract(typeof(SetMultipleSettingsCommandArguments))]
        SetMultipleSettings,

        // Push notifications that invalidate or update live frontend state.
        [NativeMessageContract(ResponseType = typeof(WorkingTreeChangedNotification))]
        WorkingTreeChanged,
        [NativeMessageContract(ResponseType = typeof(CommitGraphChangedNotification))]
        CommitGraphChanged,
        [NativeMessageContract(ResponseType = typeof(CloneRepositoryProgressNotification))]
        CloneRepositoryProgress
    }
}
