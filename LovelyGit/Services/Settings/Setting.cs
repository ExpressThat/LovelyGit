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
        CommitDiffWrapLines,
        AiFeaturesEnabled,
        AiComputeDevice,
        AiModel,
        AiContextSize,
        AiLlamaRawDiffContextPercent,
        AiGemmaRawDiffContextPercent,
        AiSummaryContextPercent
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

    [TranspilationSource]
    [JsonConverter(typeof(JsonStringEnumConverter<AiComputeDevice>))]
    public enum AiComputeDevice
    {
        Cpu,
        Gpu
    }

    [TranspilationSource]
    [JsonConverter(typeof(JsonStringEnumConverter<AiModel>))]
    public enum AiModel
    {
        Llama32_1B,
        Llama32_3B,
        Gemma4_E2B,
        Gemma4_E4B
    }
}
