using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

[TypeSharp]
public sealed record CreateTagAtCommitCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string CommitHash { get; init; } = string.Empty;
    public string TagName { get; init; } = string.Empty;
    public bool IsAnnotated { get; init; }
    public bool Sign { get; init; }
    public string? Message { get; init; }
}

[TypeSharp]
public sealed record DeleteTagCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string TagName { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record PushTagCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string RemoteName { get; init; } = string.Empty;
    public string TagName { get; init; } = string.Empty;
}

[TypeSharp]
public sealed record DeleteRemoteTagCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string RemoteName { get; init; } = string.Empty;
    public string TagName { get; init; } = string.Empty;
}
