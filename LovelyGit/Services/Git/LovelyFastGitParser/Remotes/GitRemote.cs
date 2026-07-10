using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

[TypeSharp]
public record GitRemote
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? PushUrl { get; set; }
}
