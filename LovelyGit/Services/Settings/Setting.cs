using System.Text.Json.Serialization;
using Tapper;

namespace ExpressThat.LovelyGit.Services.Settings
{
    [TranspilationSource]
    [JsonConverter(typeof(JsonStringEnumConverter<Setting>))]
    public enum Setting
    {
        CurrentGitRepositoryId,
        Theme,
        CommitDiffViewMode,
        CommitDiffWrapLines
    }

    [TranspilationSource]
    [JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
    public enum AppTheme
    {
        System,
        Light,
        Dark
    }
}
