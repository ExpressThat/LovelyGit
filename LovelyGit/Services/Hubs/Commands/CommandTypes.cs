using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    [TranspilationSource]
    public enum CommsHubCommandType
    {
        KnownGitRepositorys,
        CommitGraph,
        GetSetting,
        SetSetting,
        GetAllSettings
    }
}
