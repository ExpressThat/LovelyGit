namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

public record GitRemote
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}
