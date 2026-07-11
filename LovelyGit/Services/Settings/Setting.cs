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
        LightTheme,
        DarkTheme,
        Font,
        UiFont,
        CodeFont,
        LightUiFont,
        LightCodeFont,
        DarkUiFont,
        DarkCodeFont,
        LightAccent,
        LightBackground,
        LightForeground,
        DarkAccent,
        DarkBackground,
        DarkForeground,
        CommitDiffViewMode,
        CommitDiffLineDisplayMode,
        CommitDiffContextLines,
        CommitDiffWrapLines,
        CommitDiffIgnoreWhitespace,
        CommitGraphRefsPanelOpen,
        SignCommitsByDefault,
        RemotePrimaryAction
    }

    [TypeSharp]
    [Union]
    [JsonConverter(typeof(JsonStringEnumConverter<CommitDiffLineDisplayMode>))]
    public enum CommitDiffLineDisplayMode
    {
        Changes,
        FullFile
    }

    [TypeSharp]
    [Union]
    [JsonConverter(typeof(JsonStringEnumConverter<RemotePrimaryAction>))]
    public enum RemotePrimaryAction
    {
        Fetch,
        Pull,
        PullRebase,
        PullFastForwardOnly,
        Push
    }

}
