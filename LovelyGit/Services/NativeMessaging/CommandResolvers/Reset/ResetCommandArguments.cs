using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;

[TypeSharp]
public sealed record ResetCurrentBranchToCommitCommandArguments
{
    public Guid RepositoryId { get; init; }
    public string CommitHash { get; init; } = string.Empty;
    public GitResetMode ResetMode { get; init; }
}
