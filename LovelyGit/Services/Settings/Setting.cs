using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Settings
{
    [TypeSharp]
    [Union]
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

    [TypeSharp]
    [Union]
    [JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
    public enum AppTheme
    {
        System,
        Light,
        Dark
    }

    [TypeSharp]
    [Union]
    [JsonConverter(typeof(JsonStringEnumConverter<CommitDiffLineDisplayMode>))]
    public enum CommitDiffLineDisplayMode
    {
        Changes,
        FullFile
    }

}
