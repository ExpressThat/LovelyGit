using ExpressThat.LovelyGit.Services.Git.Bisect;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Bisect;

[TypeSharp]
public sealed record GetBisectStateCommandArguments
{
    public Guid RepositoryId { get; set; }
}

[TypeSharp]
public sealed record ManageBisectCommandArguments
{
    public Guid RepositoryId { get; set; }
    public GitBisectAction Action { get; set; }
    public string? GoodCommit { get; set; }
}
