using System.Text.Json.Serialization;
using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    [TranspilationSource]
    [JsonConverter(typeof(JsonStringEnumConverter<CommsHubCommandType>))]
    public enum CommsHubCommandType
    {
        KnownGitRepositorys,
        AddKnownGitRepositorys,
        RemoveKnownGitRepositorys,
        CommitGraph,
        GetCommitDetails,
        GetCommitFileDiff,
        GetWorkingTreeChangeSummary,
        GetWorkingTreeChanges,
        GetWorkingTreeFileDiff,
        StageWorkingTreeFiles,
        UnstageWorkingTreeFiles,
        StageWorkingTreeLine,
        UnstageWorkingTreeLine,
        CommitStagedChanges,
        FetchRepository,
        PushRepository,
        CancelCommitDiffPreparation,
        GetSetting,
        SetSetting,
        GetAllSettings,
        SetMultipleSettings,
        GenerateCommitMessage,
        GetAiModelLicenses
    }
}
