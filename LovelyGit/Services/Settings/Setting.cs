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
        CommitDiffLineDisplayMode,
        CommitDiffContextLines,
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

    [TranspilationSource]
    [JsonConverter(typeof(JsonStringEnumConverter<CommitDiffLineDisplayMode>))]
    public enum CommitDiffLineDisplayMode
    {
        Changes,
        FullFile
    }
}
