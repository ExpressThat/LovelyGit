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
        CommitGraph,
        GetSetting,
        SetSetting,
        GetAllSettings,
        SetMultipleSettings
    }
}
