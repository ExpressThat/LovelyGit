using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

[TypeSharp]
public record GitRemote
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? PushUrl { get; set; }
}

[TypeSharp]
public sealed record GetRemotesResponse
{
    public List<GitRemote> Remotes { get; init; } = [];

    public string? CompactRemotesGzipBase64 { get; init; }
}
