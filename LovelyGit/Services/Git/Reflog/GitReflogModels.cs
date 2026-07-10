using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.Reflog;

[TypeSharp]
public sealed record GitReflogResponse
{
    public string ReferenceName { get; init; } = string.Empty;
    public List<GitReflogEntry> Entries { get; init; } = new();
}

[TypeSharp]
public sealed record GitReflogEntry
{
    public string Selector { get; init; } = string.Empty;
    public string OldHash { get; init; } = string.Empty;
    public string NewHash { get; init; } = string.Empty;
    public string ActorName { get; init; } = string.Empty;
    public string ActorEmail { get; init; } = string.Empty;
    public long TimestampUnixSeconds { get; init; }
    public string Timezone { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
