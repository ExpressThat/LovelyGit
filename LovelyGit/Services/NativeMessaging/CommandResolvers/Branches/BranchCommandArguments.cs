using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

[TypeSharp]
public record CreateBranchFromCommitCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string CommitHash { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
}

[TypeSharp]
public record RenameBranchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string OldBranchName { get; set; } = string.Empty;
    public string NewBranchName { get; set; } = string.Empty;
}

[TypeSharp]
public record DeleteBranchCommandArguments
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool Force { get; set; }
}
