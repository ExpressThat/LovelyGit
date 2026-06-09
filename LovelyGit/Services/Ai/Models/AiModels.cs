using Tapper;
using ExpressThat.LovelyGit.Services.Settings;

namespace ExpressThat.LovelyGit.Services.Ai.Models;

[TranspilationSource]
public sealed record GenerateCommitMessageCommandArguments
{
    public Guid RepositoryId { get; set; }
}

[TranspilationSource]
public sealed record GenerateCommitMessageResponse
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public AiComputeDevice ComputeDevice { get; set; }
    public int ContextSize { get; set; }
    public int GpuLayerCount { get; set; }
}

[TranspilationSource]
public sealed record AiModelDownloadProgressNotification
{
    public string Model { get; set; } = string.Empty;
    public long BytesReceived { get; set; }
    public long? TotalBytes { get; set; }
    public double? Percent { get; set; }
    public bool IsComplete { get; set; }
}

[TranspilationSource]
public sealed record GetAiModelLicensesCommandArguments
{
}

[TranspilationSource]
public sealed record AiModelLicenseInfo
{
    public AiModel Model { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string RepositoryId { get; set; } = string.Empty;
    public string LicenseName { get; set; } = string.Empty;
    public string LicenseUrl { get; set; } = string.Empty;
    public string LicenseText { get; set; } = string.Empty;
    public bool IsCached { get; set; }
}

[TranspilationSource]
public sealed record GetAiModelLicensesResponse
{
    public IReadOnlyList<AiModelLicenseInfo> Licenses { get; set; } = [];
}
