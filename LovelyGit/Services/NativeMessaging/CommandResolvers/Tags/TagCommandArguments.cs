using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

[TypeSharp]
public record CreateTagAtCommitCommandArguments
{
    public Guid RepositoryId { get; set; }

    public string CommitHash { get; set; } = string.Empty;

    public string TagName { get; set; } = string.Empty;
}

[TypeSharp]
public record DeleteTagCommandArguments
{
    public Guid RepositoryId { get; set; }

    public string TagName { get; set; } = string.Empty;
}
