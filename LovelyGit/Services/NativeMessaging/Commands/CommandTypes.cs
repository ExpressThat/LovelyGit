using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Settings;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.Settings;
using ExpressThat.LovelyGit.Services.TypeGeneration;
using System.Text.Json;

namespace ExpressThat.LovelyGit.Services.NativeMessaging
{
    [TypeSharp]
    [JsonConverter(typeof(JsonStringEnumConverter<NativeMessageType>))]
    public enum NativeMessageType
    {
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(List<KnownGitRepository>))]
        KnownGitRepositorys,
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(KnownGitRepository))]
        AddKnownGitRepositorys,
        [NativeMessageContract(typeof(RemoveKnownGitRepositorysCommandArguments))]
        RemoveKnownGitRepositorys,
        [NativeMessageContract(typeof(CommitGraphCommandArguments), typeof(CommitGraphResponse))]
        CommitGraph,
        [NativeMessageContract(typeof(GetCommitDetailsCommandArguments), typeof(CommitDetailsResponse))]
        GetCommitDetails,
        [NativeMessageContract(typeof(GetCommitFileDiffCommandArguments), typeof(CommitFileDiffResponse))]
        GetCommitFileDiff,
        [NativeMessageContract(typeof(GetWorkingTreeChangesCommandArguments), typeof(WorkingTreeChangeSummaryResponse))]
        GetWorkingTreeChangeSummary,
        [NativeMessageContract(typeof(GetWorkingTreeChangesCommandArguments), typeof(WorkingTreeChangesResponse))]
        GetWorkingTreeChanges,
        [NativeMessageContract(typeof(GetWorkingTreeFileDiffArguments), typeof(CommitFileDiffResponse))]
        GetWorkingTreeFileDiff,
        [NativeMessageContract(typeof(UpdateWorkingTreeIndexCommandArguments))]
        StageWorkingTreeFiles,
        [NativeMessageContract(typeof(UpdateWorkingTreeIndexCommandArguments))]
        UnstageWorkingTreeFiles,
        [NativeMessageContract(typeof(StageWorkingTreeLineCommandArguments))]
        StageWorkingTreeLine,
        [NativeMessageContract(typeof(StageWorkingTreeLineCommandArguments))]
        UnstageWorkingTreeLine,
        [NativeMessageContract(typeof(CommitStagedChangesCommandArguments))]
        CommitStagedChanges,
        [NativeMessageContract(typeof(GitRemoteCommandArguments))]
        FetchRepository,
        [NativeMessageContract(typeof(GitRemoteCommandArguments))]
        PushRepository,
        [NativeMessageContract(typeof(CancelCommitDiffPreparationCommandArguments))]
        CancelCommitDiffPreparation,
        [NativeMessageContract(typeof(GetSettingsCommandArguments), typeof(JsonElement))]
        GetSetting,
        [NativeMessageContract(typeof(SetSettingsCommandArguments))]
        SetSetting,
        [NativeMessageContract(typeof(EmptyCommandArguments), typeof(Dictionary<Setting, JsonElement>))]
        GetAllSettings,
        [NativeMessageContract(typeof(SetMultipleSettingsCommandArguments))]
        SetMultipleSettings,
        [NativeMessageContract(ResponseType = typeof(WorkingTreeChangedNotification))]
        WorkingTreeChanged,
        [NativeMessageContract(ResponseType = typeof(CommitGraphChangedNotification))]
        CommitGraphChanged
    }
}
