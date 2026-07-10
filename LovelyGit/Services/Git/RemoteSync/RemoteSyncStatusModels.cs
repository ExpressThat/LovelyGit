using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.RemoteSync;

[TypeSharp]
public sealed record GetRemoteSyncStatusCommandArguments
{
    public Guid RepositoryId { get; init; }
}

[TypeSharp]
public sealed record RemoteSyncStatusResponse
{
    public string? BranchName { get; init; }
    public string? UpstreamName { get; init; }
    public string? LocalHash { get; init; }
    public string? UpstreamHash { get; init; }
    public int AheadCount { get; init; }
    public int BehindCount { get; init; }
    public bool HasUpstream { get; init; }
    public bool IsUpstreamAvailable { get; init; }
    public bool IsHistoryPartial { get; init; }
}
