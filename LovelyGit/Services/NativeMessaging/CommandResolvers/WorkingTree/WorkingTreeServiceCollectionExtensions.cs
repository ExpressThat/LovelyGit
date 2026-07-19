using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.Rebase;
using ExpressThat.LovelyGit.Services.Git.Patches;
using ExpressThat.LovelyGit.Services.Git.Submodules;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.RepositoryOperations;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Revert;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal static class WorkingTreeServiceCollectionExtensions
{
    public static IServiceCollection AddWorkingTreeCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(WorkingTreeJsonSerializerContext.Default);
        services.AddLovelyGitJsonTypeInfoResolver(BranchesJsonSerializerContext.Default);
        services.AddLovelyGitJsonTypeInfoResolver(CherryPickJsonSerializerContext.Default);
        services.AddLovelyGitJsonTypeInfoResolver(CheckoutJsonSerializerContext.Default);
        services.AddLovelyGitJsonTypeInfoResolver(MergeJsonSerializerContext.Default);
        services.AddLovelyGitJsonTypeInfoResolver(RebaseJsonSerializerContext.Default);
        services.AddLovelyGitJsonTypeInfoResolver(RepositoryOperationsJsonSerializerContext.Default);
        services.AddLovelyGitJsonTypeInfoResolver(RevertJsonSerializerContext.Default);
        services.AddLovelyGitJsonTypeInfoResolver(ResetJsonSerializerContext.Default);
        services.AddLovelyGitJsonTypeInfoResolver(TagsJsonSerializerContext.Default);
        services.AddSingleton<WorkingTreeChangeService>();
        services.AddSingleton<ConflictResolutionService>();
        services.AddSingleton<ConflictExternalMergeToolService>();
        services.AddSingleton<GitInteractiveRebaseService>();
        services.AddSingleton<WorkingTreeStatusListService>();
        services.AddSingleton<WorkingTreePreliminarySummaryService>();
        services.AddSingleton<WorkingTreeSummaryService>();
        services.AddSingleton<GitMaintenanceScheduler>();
        services.AddSingleton<IGitMaintenanceScheduler>(provider =>
            provider.GetRequiredService<GitMaintenanceScheduler>());
        services.AddHostedService(provider =>
            provider.GetRequiredService<GitMaintenanceScheduler>());
        services.AddSingleton<WorkingTreeIndexService>();
        services.AddSingleton<GitIgnoreService>();
        services.AddSingleton<HeadCommitMessageService>();
        services.AddSingleton<UndoLastCommitService>();
        services.AddSingleton<PatchPreviewService>();
        services.AddSingleton<PatchApplyService>();
        services.AddSingleton<NativeSubmoduleReader>();
        services.AddSingleton<SubmoduleCommandService>();
        services.AddSingleton<WorkingTreeWatcherSuppressionCoordinator>();
        services.AddSingleton<WorkingTreeWatcherService>();
        services.AddHostedService<ActiveRepositorySettingsWatcher>();
        services.AddSingleton<ICommandResponder, GetWorkingTreeChangesCommandResolver>();
        services.AddSingleton<ICommandResponder, GetWorkingTreeChangeSummaryCommandResolver>();
        services.AddSingleton<ICommandResponder, GetWorkingTreeFileDiffCommandResolver>();
        services.AddSingleton<ICommandResponder, GetConflictResolutionCommandResolver>();
        services.AddSingleton<ICommandResponder, ResolveConflictCommandResolver>();
        services.AddSingleton<ICommandResponder, OpenConflictInMergeToolCommandResolver>();
        services.AddSingleton<ICommandResponder, RevealWorkingTreeFileCommandResolver>();
        services.AddSingleton<ICommandResponder, IgnoreWorkingTreePathCommandResolver>();
        services.AddSingleton<ICommandResponder, StageWorkingTreeFilesCommandResolver>();
        services.AddSingleton<ICommandResponder, UnstageWorkingTreeFilesCommandResolver>();
        services.AddSingleton<ICommandResponder, DiscardWorkingTreeChangesCommandResolver>();
        services.AddSingleton<ICommandResponder, StageWorkingTreeLineCommandResolver>();
        services.AddSingleton<ICommandResponder, UnstageWorkingTreeLineCommandResolver>();
        services.AddSingleton<ICommandResponder, WorkingTreeHunkCommandResolver>();
        services.AddSingleton<ICommandResponder, GetHeadCommitMessageCommandResolver>();
        services.AddSingleton<ICommandResponder, UndoLastCommitCommandResolver>();
        services.AddSingleton<ICommandResponder, CommitStagedChangesCommandResolver>();
        services.AddSingleton<ICommandResponder, ChoosePatchFileCommandResolver>();
        services.AddSingleton<ICommandResponder, ApplyPatchCommandResolver>();
        services.AddSingleton<ICommandResponder, GetSubmodulesCommandResolver>();
        services.AddSingleton<ICommandResponder, ManageSubmoduleCommandResolver>();
        services.AddSingleton<ICommandResponder, ChooseWorktreeDestinationCommandResolver>();
        services.AddSingleton<ICommandResponder, CreateWorktreeCommandResolver>();
        services.AddSingleton<ICommandResponder, ManageWorktreeCommandResolver>();
        services.AddSingleton<ICommandResponder, GetRemotesCommandResolver>();
        services.AddSingleton<ICommandResponder, GetRemoteSyncStatusCommandResolver>();
        services.AddSingleton<ICommandResponder, ManageRemoteCommandResolver>();
        services.AddSingleton<ICommandResponder, FetchRepositoryCommandResolver>();
        services.AddSingleton<ICommandResponder, PullRepositoryCommandResolver>();
        services.AddSingleton<ICommandResponder, PushRepositoryCommandResolver>();
        services.AddSingleton<ICommandResponder, CheckoutBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, CheckoutCommitCommandResolver>();
        services.AddSingleton<ICommandResponder, CheckoutTagCommandResolver>();
        services.AddSingleton<ICommandResponder, CheckoutRemoteBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, CreateBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, RenameBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, DeleteBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, DeleteRemoteBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, PushBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, ManageBranchUpstreamCommandResolver>();
        services.AddSingleton<ICommandResponder, StashCommandResolver>();
        services.AddSingleton<ICommandResponder, CherryPickCommitCommandResolver>();
        services.AddSingleton<ICommandResponder, MergeBranchIntoCurrentCommandResolver>();
        services.AddSingleton<ICommandResponder, RebaseCurrentBranchOntoBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, GetInteractiveRebasePlanCommandResolver>();
        services.AddSingleton<ICommandResponder, StartInteractiveRebaseCommandResolver>();
        services.AddSingleton<ICommandResponder, RevertCommitCommandResolver>();
        services.AddSingleton<ICommandResponder, ResetCurrentBranchToCommitCommandResolver>();
        services.AddSingleton<ICommandResponder, CreateTagAtCommitCommandResolver>();
        services.AddSingleton<ICommandResponder, DeleteTagCommandResolver>();
        services.AddSingleton<ICommandResponder, PushTagCommandResolver>();
        services.AddSingleton<ICommandResponder, DeleteRemoteTagCommandResolver>();
        services.AddSingleton<ICommandResponder, GetRepositoryOperationStateCommandResolver>();
        services.AddSingleton<ICommandResponder, ContinueRepositoryOperationCommandResolver>();
        services.AddSingleton<ICommandResponder, AbortRepositoryOperationCommandResolver>();

        return services;
    }
}
