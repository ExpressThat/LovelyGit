using ExpressThat.LovelyGit.Services.Git.Lfs;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Lfs;

[TypeSharp]
public sealed record GetGitLfsStateCommandArguments
{
    public Guid RepositoryId { get; init; }
}

[TypeSharp]
public sealed record ManageGitLfsCommandArguments
{
    public Guid RepositoryId { get; init; }
    public GitLfsAction Action { get; init; }
    public string? Pattern { get; init; }
}
